namespace VentureSketch.Controllers
{
    using System.Collections.Generic;
    using System.Configuration;
    using System.Diagnostics;
    using System.Data;
    using System.Linq;
    using System.Text;
    using System.Web.Mvc;
    using VentureSketch.Constants;
    using VentureSketch.Framework;
    using VentureSketch.Services;
    using VentureSketch.Data;
    using VentureSketch.Models;

    public class AdminController : Controller
    {
        #region Constructors
        public AdminController()
        {
        }
        #endregion

        [Route("Admin", Name = AdminControllerRoute.GetIndex)]
        public ActionResult Index()
        {
            return this.View(AdminControllerAction.Index);
        }

        [Route("Admin/Qualifications", Name = AdminControllerRoute.GetQualifications)]
        public ActionResult Qualifications()
        {
            IEnumerable<Qualification> qualifications = new List<Qualification>();

            using (GenericRepository<Qualification> qualificationRepository = new GenericRepository<Qualification>(ConfigurationManager.ConnectionStrings["VentureSketchConnectionString"].ConnectionString))
            {
                qualifications = qualificationRepository.List();
            }
            
            return this.View(AdminControllerAction.Qualifications, qualifications.ToList());
        }

        
        [Route("Admin/EditQualification", Name = AdminControllerRoute.EditQualification)]
        public ActionResult EditQualification(int id)
        {
            Qualification qualification = null;

            using (GenericRepository<Qualification> qualificationRepository = new GenericRepository<Qualification>(ConfigurationManager.ConnectionStrings["VentureSketchConnectionString"].ConnectionString))
            {
                qualification = qualificationRepository.Find(id);
            }

            return this.PartialView(AdminControllerAction.EditQualification, qualification);
        }

    }
}