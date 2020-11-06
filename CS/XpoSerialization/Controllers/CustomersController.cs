using DevExpress.Xpo;
using Microsoft.AspNetCore.Mvc;
using System;
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
        public IActionResult Post([FromBody] Customer customer) {
            try {
                uow.CommitChanges();
                return NoContent();
            } catch(Exception exception) {
                return BadRequest(exception);
            }
        }
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Customer customer) {
            if(id != customer.Oid)
                return NotFound();
            try {
                uow.CommitChanges();
                return NoContent();
            } catch(Exception exception) {
                return BadRequest(exception);
            }
        }
        [HttpDelete("{id}")]
        public IActionResult Delete(int id) {
            try {
                Customer customer = uow.GetObjectByKey<Customer>(id);
                uow.Delete(customer);
                uow.CommitChanges();
                return NoContent();
            } catch(Exception exception) {
                return BadRequest(exception);
            }
        }
    }
}
