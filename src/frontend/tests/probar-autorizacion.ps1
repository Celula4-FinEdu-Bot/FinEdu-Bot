<#
.SYNOPSIS
    Prueba manual de autorización por rol (HU-40) contra el frontend en ejecución.

.DESCRIPTION
    Solo prueba rutas de página Blazor. Los controladores /api no están mapeados
    (no hay AddControllers/MapControllers en Program.cs), así que no se prueban aquí.

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

# Lee la cabecera Location tanto en Windows PowerShell 5.1 como en PowerShell 7.
function Get-LocationHeader($Resp) {
    if ($null -eq $Resp) { return '' }
    if ($Resp -is [System.Net.HttpWebResponse]) { return [string]$Resp.Headers['Location'] }
    if ($Resp.Headers -and $Resp.Headers.Location) { return [string]$Resp.Headers.Location }
    if ($Resp.Headers -and $Resp.Headers['Location']) { return [string]($Resp.Headers['Location'] | Select-Object -First 1) }
    return ''
}

# Devuelve @{ Status; Location; Body } aunque la respuesta sea 3xx/4xx/5xx.
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
        # En PowerShell 5.1, un 3xx con MaximumRedirection 0 es un error no terminante que
        # igual devuelve la respuesta; con 'Stop' se perdería el código y la cabecera Location.
        ErrorAction     = 'SilentlyContinue'
        ErrorVariable   = 'probeError'
    }
    if ($Session) { $params.WebSession = $Session }
    if ($null -ne $Body) { $params.Body = $Body }
    if ($ContentType) { $params.ContentType = $ContentType }
    if (-not $FollowRedirects) { $params.MaximumRedirection = 0 }

    try {
        $r = Invoke-WebRequest @params
    }
    catch {
        $probeError = @($_)
    }

    if ($null -ne $r) {
        return @{ Status = [int]$r.StatusCode; Location = (Get-LocationHeader $r); Body = [string]$r.Content }
    }

    $err = $probeError | Select-Object -First 1
    $resp = if ($err) { $err.Exception.Response } else { $null }
    if ($null -eq $resp) {
        $msg = if ($err) { $err.Exception.Message } else { 'sin respuesta' }
        return @{ Status = 0; Location = ''; Body = $msg }
    }

    $body = ''
    if ($err.ErrorDetails -and $err.ErrorDetails.Message) {
        $body = $err.ErrorDetails.Message
    }
    elseif ($resp -is [System.Net.HttpWebResponse]) {
        try {
            $reader = New-Object System.IO.StreamReader($resp.GetResponseStream())
            $body = $reader.ReadToEnd()
        } catch { }
    }
    return @{ Status = [int]$resp.StatusCode; Location = (Get-LocationHeader $resp); Body = $body }
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

function Format-Obtenido($Res) {
    $loc = if ($Res.Location) { $Res.Location } else { '(sin Location)' }
    return "obtenido $($Res.Status), Location: $loc"
}

function Test-Redireccion([string]$Caso, $Res, [string]$Destino) {
    $ok = $Res.Status -eq 302 -and $Res.Location -like "*$Destino*"
    Write-Resultado $Caso $ok "esperado 302 hacia $Destino, $(Format-Obtenido $Res)"
}

function Test-Ok200([string]$Caso, $Res) {
    Write-Resultado $Caso ($Res.Status -eq 200) "esperado 200, $(Format-Obtenido $Res)"
}

function Test-Html([string]$Caso, $Res, [string]$Texto) {
    $contiene = $Res.Body -match [regex]::Escape($Texto)
    $obtenido = if ($contiene) { 'contiene' } else { 'no contiene' }
    Write-Resultado $Caso $contiene "esperado que contenga '$Texto', obtenido '$obtenido' (HTTP $($Res.Status))"
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
Test-Redireccion 'GET /proyectos' (Invoke-Probe -Path '/proyectos') '/login'
Test-Redireccion 'GET /presupuesto' (Invoke-Probe -Path '/presupuesto') '/login'

Write-Host "`n== USER ($NormalUser)" -ForegroundColor Cyan
$userSession = New-SesionAutenticada $NormalUser $userPass
Test-Redireccion 'GET /proyectos' (Invoke-Probe -Path '/proyectos' -Session $userSession) '/acceso-denegado'
Test-Ok200 'GET /presupuesto' (Invoke-Probe -Path '/presupuesto' -Session $userSession)
Test-Html 'GET /acceso-denegado' (Invoke-Probe -Path '/acceso-denegado' -Session $userSession -FollowRedirects) 'Acceso denegado'

Write-Host "`n== ADMIN ($AdminUser)" -ForegroundColor Cyan
$adminSession = New-SesionAutenticada $AdminUser $adminPass
Test-Ok200 'GET /proyectos' (Invoke-Probe -Path '/proyectos' -Session $adminSession)
Test-Ok200 'GET /presupuesto' (Invoke-Probe -Path '/presupuesto' -Session $adminSession)

Write-Host ''
if ($script:Fallos -eq 0) {
    Write-Host 'Todas las pruebas pasaron.' -ForegroundColor Green
    exit 0
}
Write-Host "$($script:Fallos) prueba(s) fallaron." -ForegroundColor Red
exit 1
