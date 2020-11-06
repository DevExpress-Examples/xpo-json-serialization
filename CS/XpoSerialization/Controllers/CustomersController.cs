using DevExpress.Xpo;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
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
        public IEnumerable Get() {
            return uow.Query<Customer>();
        }
        [HttpGet("{id}")]
        public Customer Get(int id) {
            return uow.GetObjectByKey<Customer>(id);
        }
        [HttpPost]
        public void Post([FromBody] Customer customer) {
            uow.CommitChanges();
        }
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] Customer customer) {
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
