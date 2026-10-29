<#
.SYNOPSIS
    Prueba manual de autorización por rol (HU-40) contra el frontend en ejecución.

.EXAMPLE
    .\probar-autorizacion.ps1 -AdminUser admin -NormalUser usuario
    .\probar-autorizacion.ps1 -BaseUrl https://localhost:5001 -AdminUser admin -NormalUser usuario

    Las contraseñas se piden por consola; nunca se guardan en este archivo.
#>
param(
    [string]$BaseUrl = 'http://localhost:5000',
    [Parameter(Mandatory = $true)][string]$AdminUser,
    [Parameter(Mandatory = $true)][string]$NormalUser
)

$BaseUrl = $BaseUrl.TrimEnd('/')
$script:Fallos = 0

function ConvertTo-PlainText([Security.SecureString]$Secure) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Secure)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

# Devuelve @{ Status; Body } aunque la respuesta sea 3xx/4xx/5xx.
function Invoke-Probe {
    param(
        [string]$Method = 'GET',
        [string]$Path,
        [Microsoft.PowerShell.Commands.WebRequestSession]$Session,
        $Body,
        [string]$ContentType,
        [switch]$FollowRedirects
    )

    $params = @{
        Uri             = "$BaseUrl$Path"
        Method          = $Method
        UseBasicParsing = $true
        ErrorAction     = 'Stop'
    }
    if ($Session) { $params.WebSession = $Session }
    if ($null -ne $Body) { $params.Body = $Body }
    if ($ContentType) { $params.ContentType = $ContentType }
    if (-not $FollowRedirects) { $params.MaximumRedirection = 0 }

    try {
        $r = Invoke-WebRequest @params
        return @{ Status = [int]$r.StatusCode; Body = [string]$r.Content }
    }
    catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) { return @{ Status = 0; Body = $_.Exception.Message } }

        $body = ''
        if ($_.ErrorDetails -and $_.ErrorDetails.Message) {
            $body = $_.ErrorDetails.Message
        }
        elseif ($resp -is [System.Net.HttpWebResponse]) {
            try {
                $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
                $body = $reader.ReadToEnd()
            } catch { }
        }
        return @{ Status = [int]$resp.StatusCode; Body = $body }
    }
}

function Write-Resultado([string]$Caso, [bool]$Ok, [string]$Detalle) {
    if ($Ok) {
        Write-Host "[OK]    $Caso -> $Detalle" -ForegroundColor Green
    }
    else {
        Write-Host "[FALLO] $Caso -> $Detalle" -ForegroundColor Red
        $script:Fallos++
    }
}

function Test-Status([string]$Caso, $Res, [int]$Esperado) {
    Write-Resultado $Caso ($Res.Status -eq $Esperado) "esperado $Esperado, obtenido $($Res.Status)"
}

function Test-Permitido([string]$Caso, $Res) {
    # 404 significa que la ruta no existe, así que tampoco cuenta como acceso permitido.
    $ok = $Res.Status -ne 0 -and $Res.Status -notin 401, 403, 404
    Write-Resultado $Caso $ok "esperado distinto de 401/403, obtenido $($Res.Status)"
}

function Test-Html([string]$Caso, $Res, [bool]$DebeContener) {
    $contiene = $Res.Body -match 'Acceso denegado'
    $esperado = if ($DebeContener) { 'contiene' } else { 'no contiene' }
    $obtenido = if ($contiene) { 'contiene' } else { 'no contiene' }
    Write-Resultado $Caso ($contiene -eq $DebeContener) "esperado '$esperado Acceso denegado', obtenido '$obtenido' (HTTP $($Res.Status))"
}

function New-SesionAutenticada([string]$Usuario, [Security.SecureString]$Clave) {
    $session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
    $form = @{ username = $Usuario; password = (ConvertTo-PlainText $Clave) }

    $res = Invoke-Probe -Method POST -Path '/account/login' -Session $session `
        -Body $form -ContentType 'application/x-www-form-urlencoded'

    $cookie = $session.Cookies.GetCookies([Uri]$BaseUrl) | Where-Object Name -eq 'FinEdu.Auth'
    Write-Resultado "Login $Usuario" ($null -ne $cookie) "HTTP $($res.Status), cookie FinEdu.Auth $(if ($cookie) { 'recibida' } else { 'NO recibida' })"
    return $session
}

$adminPass = Read-Host "Contraseña de $AdminUser (ADMIN)" -AsSecureString
$userPass = Read-Host "Contraseña de $NormalUser (USER)" -AsSecureString

Write-Host "`n== Sin sesión ($BaseUrl)" -ForegroundColor Cyan
Test-Status 'GET /api/mef/total' (Invoke-Probe -Path '/api/mef/total') 401
Test-Status 'GET /api/mef/proyectos' (Invoke-Probe -Path '/api/mef/proyectos') 401
Test-Status 'POST /api/nlq' (Invoke-Probe -Method POST -Path '/api/nlq' -Body '{"query":"prueba"}' -ContentType 'application/json') 401

Write-Host "`n== USER ($NormalUser)" -ForegroundColor Cyan
$userSession = New-SesionAutenticada $NormalUser $userPass
Test-Permitido 'GET /api/mef/total' (Invoke-Probe -Path '/api/mef/total' -Session $userSession)
Test-Status 'GET /api/mef/proyectos' (Invoke-Probe -Path '/api/mef/proyectos' -Session $userSession) 403
Test-Html 'GET /proyectos' (Invoke-Probe -Path '/proyectos' -Session $userSession -FollowRedirects) $true

Write-Host "`n== ADMIN ($AdminUser)" -ForegroundColor Cyan
$adminSession = New-SesionAutenticada $AdminUser $adminPass
Test-Permitido 'GET /api/mef/proyectos' (Invoke-Probe -Path '/api/mef/proyectos' -Session $adminSession)
Test-Html 'GET /proyectos' (Invoke-Probe -Path '/proyectos' -Session $adminSession -FollowRedirects) $false

Write-Host ''
if ($script:Fallos -eq 0) {
    Write-Host 'Todas las pruebas pasaron.' -ForegroundColor Green
    exit 0
}
Write-Host "$($script:Fallos) prueba(s) fallaron." -ForegroundColor Red
exit 1
