using DevExpress.Xpo;
using DevExpress.Xpo.Helpers;

using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

using XpoSerialization.DxSampleModel;

namespace XpoSerialization.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase {
        private UnitOfWork uow;
        public CustomersController(UnitOfWork uow) {
            this.uow = uow;
        }
        [HttpGet]
        public async Task<IEnumerable<Customer>> Get() {
            return await uow.Query<Customer>().ToArrayAsync();
        }
        [HttpGet("{id}")]
        public async Task<Customer> Get(int id) {
            return await uow.GetObjectByKeyAsync<Customer>(id);
        }
        [HttpPost]
        public async Task<Customer> Post([FromBody] ChangesSet<Customer> customerData) {
            var customer = new Customer(uow);
            customerData.Put(uow, customer);
            await uow.CommitChangesAsync();
            return customer;
        }
        [HttpPut("{id}")]
        public async Task<ActionResult<Customer>> Put(int id, [FromBody] ChangesSet<Customer> customerData) {
            var customer = uow.GetObjectByKey<Customer>(id);
            if(customer == null) {
                return NotFound();
            }
            customerData.Patch(uow, customer);
            await uow.CommitChangesAsync();
            return customer;
        }
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id) {
            var customer = await uow.GetObjectByKeyAsync<Customer>(id);
            if(customer == null) {
                return NotFound();
            }
            uow.Delete(customer);
            uow.CommitChanges();
            return NoContent();
        }
    }
}
