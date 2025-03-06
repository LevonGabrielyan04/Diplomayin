using Diplomayin.Models;
using Microsoft.AspNetCore.Mvc;

namespace Diplomayin.Controllers
{
    // PoliciesController.cs
    public class PoliciesController : Controller
    {
        public static List<Policy> _policies = new List<Policy>();

        // GET: Policies
        public ActionResult Index()
        {
            return View(_policies);
        }

        // GET: Policies/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Policies/Create
        [HttpPost]
        public ActionResult Create(Policy policy)
        {
            //if (ModelState.IsValid)
            {
                policy.Id = _policies.Count + 1;
                _policies.Add(policy);
                return RedirectToAction("Index");
            }
            return View(policy);
        }
    }
}
