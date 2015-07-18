[assembly: Microsoft.Owin.OwinStartup(typeof(VentureSketch.Startup))]

namespace VentureSketch
{
    using System.Web.Mvc;
    using Owin;

    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureContainer(app);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
        }
    }
}
