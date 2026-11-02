using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using src.Security;

namespace src.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = AuthPolicies.AuthenticatedUser)]
    public class PresupuestoController : Controller
    {
        private readonly ILogger<PresupuestoController> _logger;

        public PresupuestoController(ILogger<PresupuestoController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View("Error!");
        }
    }
}