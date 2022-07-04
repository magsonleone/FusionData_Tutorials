using Autodesk.Forge;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.SystemTextJson;
using System.Text.Json.Nodes;


var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

GraphQLHttpClient? graphQLClient = null;
string? accessToken = null;
const string clientId = "<your client id";
const string clientSecret = "your client secret";
const string redirectUri = @"callback url";


/****************************
 Authentication
****************************/
#region Authentication
app.MapGet("/", async http =>
{

    if (accessToken != null)
    {
        await http.Response.WriteAsync("Got the access token. You can close the browser!");

        // Intialize GraphQL client

        graphQLClient = new GraphQLHttpClient("https://developer.api.autodesk.com/fusiondata/2022-04/graphql",
           new SystemTextJsonSerializer());
        graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + accessToken);
        //Task 1 – Pick a Hub to Work With
        await GetAllHubs();
        // Task 2 – Pick a Project to Work With
        await GetAllProjects();
        // Task 3 – Pick a Component
        await GetComponents();
        // Task 4 – Generate Thumbnails of a Component
        await GenerateThumbnail();
        // Task 5 - Download the Thumbnail
        await DownloadThumbnail();
        return;
    }

    ThreeLeggedApi threeLeggedApi = new ThreeLeggedApi();
    string authUrl = threeLeggedApi.Authorize(clientId, oAuthConstants.CODE
        , redirectUri, new Scope[] { Scope.DataCreate, Scope.DataRead, Scope.DataWrite });

    http.Response.Redirect(authUrl);

});


// Redirect
app.MapGet("/callback/oauth", async http =>
{
    ThreeLeggedApi threeLeggedApi = new ThreeLeggedApi();
    dynamic creds = await threeLeggedApi.GettokenAsync(clientId,
               clientSecret, oAuthConstants.AUTHORIZATION_CODE, http.Request.Query["code"], redirectUri);

    accessToken = creds.access_token;
    http.Response.Redirect("/");

});

#endregion End of Authentication


#region GraphQL
/////////////////////////////////////////
///Task 1 – Pick a Hub to Work With
//Function that fetches all the hubs you have access to
// Response will be in this format:
//{
//    "hubs": {
//        "results": [
//                      {
//            "name": "My first hub",
//                        "id": "a.cGVyc29uYWw6dWUyZGI0ZTE4"
//                      },
//                      {
//            "name": "My second hub",
//                        "id": "a.YnVzaW5lc3M6YXV0b2Rlc2s0ODI5"
//                      }
//                    ]
//                  }
//}
/////////////////////////////////////////
async Task GetAllHubs()
{
    var getAllHubsRequest = new GraphQLRequest
    {
        Query = @"query GetHubs {
                                  hubs {
                                    results {
                                      name
                                      id
                                    }
                                  }
                                }"
    };
    
    var response = await graphQLClient!.SendQueryAsync<JsonObject>(getAllHubsRequest);
    JsonNode? hubs = JsonNode.Parse(response.Data.ToString());
    Console.WriteLine("\n*****Listing all Hubs*****\n");
    foreach (var pair in hubs!.AsObject())
    {
        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
    }
   // var hub_id = (string?)hubs?["hubs"]!["results"]![0]!["id"];
}



/////////////////////////////////////////
///Task 2 – Pick a Project to Work With
//Function that fetches list of Projects within a Hub
// Response will be in this format:
//{
//    projects: {
//        "results": [
//                    {
//                      "name": "My First Project",
//                      "id": "a.YnVzaW5lc3M6YXV0b2Rlc2s0ODI4IzIwMjAwNDEwMjg1MTUyNTMz",
//                      "rootFolder": {
//                        "id": "urn:adsk.wipprod:fs.folder:co.-0xvwnGATSyBfM-RVhDliQ",
//                       "name": "New Project",
//                        "objectCount": 0
//                      }
//                    },
//                    {
//                      "name": "My Second Project",
//                      "id": "a.YnVzaW5lc3M6YXV0b2Rlc2s0ODI4IzIwMjAwNBEwMjg1MTUyNTU2",
//                      "rootFolder": {
//                         "id": "urn:adsk.wipprod:fs.folder:co.nmNWMmoIRRieW9wTnzVgTQ",
//                        "name": "My First Project",
//                        "objectCount": 25
//                                     }
//                    }
//                   ]
//             }
//}
/////////////////////////////////////////
async Task GetAllProjects()
{
    // use the hub id fetched from the previous method;
    var hub_id = "a.YnVzaW5lc3M6YXV0b2Rlc2s0ODI4";
    var getProjectsRequest = new GraphQLRequest
    {
        Query = @"query GetProjects ($hubId : String!) {
                          projects (hubId: $hubId) {
                            results {
                              name
                              id
                              rootFolder{
                                id
                                name
                                objectCount
                              }
                            }
                          }
                        }",
        Variables = new { hubId = hub_id }
    };

    var response = await graphQLClient!.SendQueryAsync<JsonObject>(getProjectsRequest);

    JsonNode? projects = JsonNode.Parse(response.Data.ToString());
    Console.WriteLine("\n*****Listing all Projects*****\n");
    foreach (var pair in projects!.AsObject())
    {
        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
    }
}


/////////////////////////////////////////
///Task 3 – Pick a Component
//Function that requests for a list of Components within a Folder
// Response will be in this format:
//{
//    items: {
//"results": [
//    {
//      "name": "chair v4",
//      "id": "Y29tcH5jby5iUlpxX3F3TVJWLVZCbGJ6bTdMTS1RflJrazl3Z2xhWU1qMUtyejNNNWF1dmZfYWdhfn4"
//    }
//  ]
//}
/////////////////////////////////////////
async Task GetComponents()
{
    // use the project id and the root folder id fetched from the previous method;
    var project_id = "a.YnVzaW5lc3M6YXV0b2Rlc2s0ODI4IzIwMjAwNDEwMjg1MTUyNTU2";
    var item_id = "urn:adsk.wipprod:fs.folder:co.nmNWMmoIRRicW9wTnzVgTQ";
    var getComponentRequest = new GraphQLRequest
    {
        Query = @"query GetItems($projectId: String!, $itemId: String!) {
                  items(projectId: $projectId, itemId: $itemId) {
                    results {
                      ... on Component {
                        name
                        id
                      }
                    }
                  }
                }",
        Variables = new { projectId = project_id, itemId = item_id }
    };

    var response = await graphQLClient!.SendQueryAsync<JsonObject>(getComponentRequest);
    JsonNode? components = JsonNode.Parse(response.Data.ToString());
    Console.WriteLine("\n*****Listing Components*****\n");
    foreach (var pair in components!.AsObject())
    {
        Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
    }
}


/////////////////////////////////////////
///Task 4 – Generate a Thumbnail for a Component
//Function to Extract the Component Version within the Component
// Response will be in this format:
//{
//   item: {
//      "name": "chair v4",
//      "id": "Y29tcH5jby5iUlpxX3F3TVJWLVZCbGJ6bTdMTS1RflJrazl3Z2xhWU1qMUtyejNNNWF1dmZfYWdhfn4",
//      "tipVersion": {
//      "id": "Y29tcH5jby5iUlpxX3F3TVJWLVZCbGJ6bTdMTS1RflJrazl3Z2xhWU1qMUtyejNNNWF1dmZfYWdhflZEazRmTEJMMkZON1NJdkxtQldpSEc",
//      "name": "chair v4",
//      "thumbnail": {
//        "status": "SUCCESS"
//    }
//}
//}
/////////////////////////////////////////
async Task GenerateThumbnail()
{
    // use the project id and component id fetched from the previous method;
    var project_id = "a.YnVzaW5lc3M6YXV0b2Rlc2s0ODI4IzIwMjAwNDEwMjg1MTUyNTU2";
    var item_id = "Y29tcH5jby5iUlpxX3F3TVJWLVZCbGJ6bTdMTS1RflJrazl3Z2xhWU1qMUtyejNNNWF1dmZfYWdhfn4";
    while (true)
    {
        var getCompVerRequest = new GraphQLRequest
        {
            Query = @"query Getltem($projectId: String!, $itemId: String!) {
                  item(projectId: $projectId, itemId: $itemId) {
                    name
                    ... on Component {
                      id
                      tipVersion{
                        id
                        name
                        thumbnail{
                          status
                        }
                      }
                    }
                  }
                }",
            Variables = new { projectId = project_id, itemId = item_id }
        };
        var response = await graphQLClient!.SendQueryAsync<JsonObject>(getCompVerRequest);
        JsonNode? componentVersion = JsonNode.Parse(response.Data.ToString());
        var thumbnailstatus = (string?)componentVersion?["item"]!["tipVersion"]!["thumbnail"]!["status"];
        if (thumbnailstatus == "SUCCESS")
        {
            Console.WriteLine("\n*****Listing Component Versions*****\n");
            foreach (var pair in componentVersion!.AsObject())
            {
                Console.WriteLine("{0}: {1}", pair.Key, pair.Value);
            }
            break;
        }
        Console.WriteLine("\n*****Extracting thumbnail*****\n");
        Thread.Sleep(1000); // wait for a second and then query thumbnail status;
    }
}

/////////////////////////////////////////
///Task 5 – Obtain Thumbnail URL of a Component
//Function to extract the URL of the Thumbnail and download it to temp
// Response will be in this format:
//{
//   componentVersion: {
//     "thumbnail": {
//      "status": "SUCCESS",
//      "smallImageUrl": "https://developer.api.autodesk.com/derivativeservice/v2/thumbnails/dXJuOmFkc2sud2lwcHJvZEdmVyc2lvbj0x?type=small\u0026guid=0860c483-7328-be5668858b20"
//  }
//}
/////////////////////////////////////////
async Task DownloadThumbnail()
{
    
   // use the tip version id fetched in the previous method
    var tipVersion_id = "Y29tcH5jby5iUlpxX3F3TVJWLVZCbGJ6bTdMTS1RflJrazl3Z2xhWU1qMUtyejNNNWF1dmZfYWdhflZEazRmTEJMMkZON1NJdkxtQldpSEc";
    var dwldThumbnailRequest = new GraphQLRequest
    {
        Query = @"query GetComponentVersion ($componentVersionId: String!) {
                componentVersion(componentVersionId: $componentVersionId) {
                    thumbnail {
                        status
                        smallImageUrl
                    }
                }
            }",
        Variables = new { componentVersionId = tipVersion_id }
    };

    var response = await graphQLClient!.SendQueryAsync<JsonObject>(dwldThumbnailRequest);
    JsonNode? thumbnail = JsonNode.Parse(response.Data.ToString());
    var imageUrl = (string?)thumbnail?["componentVersion"]!["thumbnail"]!["smallImageUrl"];

    HttpResponseMessage image = await graphQLClient.HttpClient.GetAsync(imageUrl);
    if (image.IsSuccessStatusCode)
    {
        var targetPath = Path.Combine(Path.GetTempPath(), "thumbnail.png");
        using FileStream target = File.OpenWrite(targetPath);
        await image.Content.CopyToAsync(target);
        Console.WriteLine($"Image downloaded to: {targetPath}");
    }
}
#endregion End of GraphQl APIs

app.Run();
