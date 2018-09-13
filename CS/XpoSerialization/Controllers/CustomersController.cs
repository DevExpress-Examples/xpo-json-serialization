using DevExpress.Xpo;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using XpoSerialization.DxSampleModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace XpoSerialization.Controllers {
    [ApiController]
    [Route("api/[controller]")]
    public class CustomersController : ControllerBase {
        private UnitOfWork uow;
        public CustomersController(UnitOfWork uow) {
            this.uow = uow;
        }
        [HttpGet]
        public IEnumerable Get() {
            return uow.Query<Customer>()
                .Select(c => new { c.Oid, c.ContactName });
        }
        [HttpGet("{id}")]
        public Customer Get(int id) {
            return uow.GetObjectByKey<Customer>(id);
        }
        [HttpPost]
        public void Post([FromBody]JObject values) {
            Customer customer = new Customer(uow);
            customer.ContactName = values["ContactName"].Value<string>();
            uow.CommitChanges();
        }
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]JObject value) {
            Customer customer = uow.GetObjectByKey<Customer>(id);
            customer.ContactName = value["ContactName"].Value<string>();
            uow.CommitChanges();
        }
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
            Customer customer = uow.GetObjectByKey<Customer>(id);
            uow.Delete(customer);
            uow.CommitChanges();
        }
    }
}
