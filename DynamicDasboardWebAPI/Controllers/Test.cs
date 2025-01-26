using DynamicDasboardWebAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly TestRepository _testRepository;

    public TestController(TestRepository testRepository)
    {
        _testRepository = testRepository;
    }

    [HttpGet("throw")]
    public IActionResult ThrowError()
    {
        throw new Exception("This is a test exception!");
    }

    [HttpGet("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        var isConnected = await _testRepository.TestConnectionAsync();
        if (isConnected)
        {
            return Ok("Database connection is successful!");
        }
        return StatusCode(500, "Database connection failed.");
    }
}
