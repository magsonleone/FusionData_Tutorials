
using Autodesk.Forge.Model;
using Autodesk.Forge;

public class Programa1
{
    private static string FORGE_CLIENT_ID = "BdQHm42Dvz5dOOVU6JiJ1Xxj2iHcL3Ks";
    private static string FORGE_CLIENT_SECRET = "9YCbioHIJ5hYGLVf";
    public static async Task Execute(string accessToken)
    {
        // Obter um token de acesso
        TwoLeggedApi oauth = new TwoLeggedApi();
        //dynamic bearer = await oauth.AuthenticateAsync(FORGE_CLIENT_ID, FORGE_CLIENT_SECRET, "client_credentials", new Scope[] { Scope.DataRead });

        // Obter a lista de projetos
        ProjectsApi projectsApi = new ProjectsApi();
        projectsApi.Configuration.AccessToken = accessToken;

        Console.WriteLine(accessToken);


        dynamic projects = await projectsApi.GetHubProjectsAsync("a.cGVyc29uYWw6dWUyOTdiODg3");

        // Imprimir a lista de projetos
        foreach (KeyValuePair<string, dynamic> project in new DynamicDictionaryItems(projects.data))
        {
            Console.WriteLine("ID: " + project.Value.id);
            Console.WriteLine("Nome: " + project.Value.attributes.name);
            //Console.WriteLine("Criado em: " + project.Value.attributes.createdDate);
            //Console.WriteLine("Atualizado em: " + project.Value.attributes.lastModifiedDate);
            Console.WriteLine("-------------------------------");
        }


        Console.ReadKey();
    }
}