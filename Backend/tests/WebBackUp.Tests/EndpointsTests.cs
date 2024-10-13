namespace WebBackUp.Tests;

public class EndpointsTests
{
    [Test]
    public async Task SaveUserData()
    {
        var app = new TestService();

        var client = app.CreateClient();

        var data = JsonSerializer.Serialize(new UserData());

        var response = await client.PostAsync("/saveuserdata/1",
            new StringContent(data, Encoding.UTF8, "application/json"));

        response.IsSuccessStatusCode.Should().BeTrue();
    }

    [Test]
    public async Task LoadUserData()
    {
        var app = new TestService();

        var client = app.CreateClient();

        var response = await client.GetAsync("/loaduserdata/1");

        var content = await response.Content.ReadAsStringAsync();

        var data = JsonSerializer.Deserialize<UserData>(content);

        data.Should().NotBeNull();
    }
}