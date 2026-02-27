using Microsoft.AspNetCore.Mvc;
using MyAspNetApp.Data;
using MyAspNetApp.Models;

namespace MyAspNetApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        // GET: api/products
        [HttpGet]
        public IActionResult GetAllProducts()
        {
            return Ok(ProductData.Products);
        }

        // GET: api/products/men
        [HttpGet("men")]
        public IActionResult GetMenProducts()
        {
            var menProducts = ProductData.Products.Where(p => p.Category == "Men").ToList();
            return Ok(menProducts);
        }

        // GET: api/products/women
        [HttpGet("women")]
        public IActionResult GetWomenProducts()
        {
            var womenProducts = ProductData.Products.Where(p => p.Category == "Women").ToList();
            return Ok(womenProducts);
        }

        // GET: api/products/{id}
        [HttpGet("{id}")]
        public IActionResult GetProductById(int id)
        {
            var product = ProductData.Products.FirstOrDefault(p => p.Id == id);
            return product != null ? Ok(product) : NotFound();
        }
    }
}
