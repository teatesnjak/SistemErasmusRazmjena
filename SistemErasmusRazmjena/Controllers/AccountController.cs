using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using SistemErasmusRazmjena.Data;
using SistemErasmusRazmjena.Models;
using SistemErasmusRazmjena.Models.ViewModels;

namespace SistemErasmusRazmjena.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<AccountController> _logger;
        private readonly ApplicationDbContext _context;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger,
            ApplicationDbContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _context = context;
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null) // Fix for CS8625: Make returnUrl nullable
        {
            _logger.LogInformation("Register GET method called. ReturnUrl: {ReturnUrl}", returnUrl);

            ViewData["ReturnUrl"] = returnUrl;

            try
            {
                var fakulteti = _context.Fakulteti.ToList();

                var model = new RegisterViewModel
                {
                    Fakulteti = fakulteti,
                    Role = "Student",
                    FakultetID = 1
                };

                return View(model); // Ensure a return statement is present to fix CS0161
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Greška se desila prilikom nabavljanja fakulteta ili pravljenja RegisterViewModel.");
                ModelState.AddModelError(string.Empty, "Greška se desila prilikom registracije. Pokušajte opet.");
                return View(new RegisterViewModel { Fakulteti = new List<Fakultet>() }); // Ensure a return statement is present to fix CS0161
            }
        }



        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            // Dump the form data to the log for debugging
            _logger.LogInformation("Form data received: {@FormData}",
                Request.Form.ToDictionary(x => x.Key, x => x.Value.ToString()));

            _logger.LogInformation("Register POST method called. Email: {Email}, Role: {Role}, FakultetID: {FakultetID}",
                model.Email, model.Role, model.FakultetID);

            // Ponovno učitaj listu fakulteta za View ako dođe do greške
            model.Fakulteti = _context.Fakulteti.ToList();

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid. Errors: {Errors}",
            string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));

                foreach (var key in ModelState.Keys)
                {
                    foreach (var error in ModelState[key].Errors)
                    {
                        _logger.LogError("Field: {Field}, Error: {Error}", key, error.ErrorMessage);
                    }
                }

                return View(model);
            }

            // Ako je Student, FakultetID je obavezan
            if (model.Role == "Student" && model.FakultetID == 0)
            {
                _logger.LogWarning("FakultetID is required for Student role but was not provided.");
                ModelState.AddModelError("FakultetID", "Fakultet je obavezan za Studente.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                Uloga = model.Role,
                FakultetID = model.Role == "Admin" ? null : model.FakultetID,
                FirstName = model.FirstName, // Ensure FirstName is set
                LastName = model.LastName    // Ensure LastName is set
            };

            _logger.LogInformation("Attempting to create user. Email: {Email}, Role: {Role}, FakultetID: {FakultetID}",
                user.Email, user.Uloga, user.FakultetID);

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User created successfully. Email: {Email}", user.Email);

                await _userManager.AddToRoleAsync(user, model.Role);
                _logger.LogInformation("User added to role: {Role}", model.Role);

                await _signInManager.SignInAsync(user, isPersistent: false);
                _logger.LogInformation("User signed in successfully. Email: {Email}", user.Email);

                return RedirectToLocal(returnUrl);
            }

            // Ako kreiranje nije uspjelo, prikaži greške
            _logger.LogWarning("User creation failed. Errors: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult RegisterKoordinator()
        {
            var fakulteti = _context.Fakulteti.ToList();
            var model = new RegisterKoordinatorViewModel
            {
                Fakulteti = fakulteti
            };

            return View(model);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterKoordinator(RegisterKoordinatorViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.FakultetID == null)
                {
                    ModelState.AddModelError("FakultetID", "Fakultet je neophodan za ECTS koordinatora.");
                    // Repopulate Fakulteti list before returning the view
                    model.Fakulteti = _context.Fakulteti.ToList();
                    return View(model);
                }

                var user = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    Uloga = "ECTSKoordinator",
                    FakultetID = model.FakultetID,
                    FirstName = model.FirstName, // Added FirstName
                    LastName = model.LastName    // Added LastName
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "ECTSKoordinator");
                    _logger.LogInformation("ECTS Koordinator stvoren od Admina.");
                    return RedirectToAction("Index", "Home");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
            }

            // Repopulate Fakulteti list before returning the view
            model.Fakulteti = _context.Fakulteti.ToList();
            return View(model);
        }




        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: false);

                if (result.Succeeded)
                {
                    _logger.LogInformation("Korisnik ulogovan.");
                    return RedirectToLocal(returnUrl);
                }

                ModelState.AddModelError(string.Empty, "Nevažeći pokušaj prijave.");
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("Korisnik izlogovan.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }
}
