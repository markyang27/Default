using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(D3.Startup))]
namespace D3
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
