using System.Linq;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.OData;
using Microsoft.WindowsAzure.Mobile.Service;
using zoompanuitService.DataObjects;
using zoompanuitService.Models;

namespace zoompanuitService.Controllers
{
    public class InputController : TableController<Input>
    {
        protected override void Initialize(HttpControllerContext controllerContext)
        {
            base.Initialize(controllerContext);
            zoompanuitContext context = new zoompanuitContext();
            DomainManager = new EntityDomainManager<Input>(context, Request, Services);
        }

        // GET tables/Input
        public IQueryable<Input> GetAllInput()
        {
            return Query(); 
        }

        // GET tables/Input/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public SingleResult<Input> GetInput(string id)
        {
            return Lookup(id);
        }

        // PATCH tables/Input/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task<Input> PatchInput(string id, Delta<Input> patch)
        {
             return UpdateAsync(id, patch);
        }

        // POST tables/Input
        public async Task<IHttpActionResult> PostInput(Input item)
        {
            Input current = await InsertAsync(item);
            return CreatedAtRoute("Tables", new { id = current.Id }, current);
        }

        // DELETE tables/Input/48D68C86-6EA6-4C25-AA33-223FC9A27959
        public Task DeleteInput(string id)
        {
             return DeleteAsync(id);
        }

    }
}