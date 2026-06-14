# QA-Final-Project

This is a repo for version control of the final project of the class "Quality Assurance"



For the project we would be using [.NET Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor).



.NET version: 10.0.300 download [here](https://dotnet.microsoft.com/es-es/download)



Intro to Blazor: [here](https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/intro)

How to run app locally once clone the project: [here](https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/run) (this is just for Blazor apps)

# How to run porject on Docker Compose!!!
can boot up the project through doker compose. 

NOTE: keycloak is NOT part of this. only for Server, Client and DB. have to add it later

1. on "/src" docker compose up --build
2. make sure to update the database again. delete all volume realated and update it (dotnet ef database update)

That's it!1


# How to run the actual project (Atleast for now until figured out Doker compose)

1. Open in VS Code the folder "InventoryManagement". In here, 3 project will load under the folder "src".
2. Open terminal, enter the folder of "src\\InventoryManagement.Server" and run dotnet watch
3. Open another terminal, enter the folder of "src\\InventoryManagement.Client" and run dotnet watch. This should open the visual of the app

# How to run database postgres(for now until docker compose)

1. Run this on terminal: docker run --name inventory -e POSTGRES_USER=admin -e POSTGRES_PASSWORD=password -e POSTGRES_DB=InventoryDb -p 5454:5432 -d postgres:18.4

2. if any errors occur that refer directly to postgres, run this inside inventorysystem.server to make sure the database is up to date: dotnet ef database update

that's it. posqgres will be running on docker and can be access through the connection made in appsetting.json on "ConnectionStrings" part

note: this is for testing and will be change later


# How to create authentication pass (for now until docker compose)

1. run the following: docker run -p 127.0.0.1:8080:8080 -e KC_BOOTSTRAP_ADMIN_USERNAME=admin -e KC_BOOTSTRAP_ADMIN_PASSWORD=admin quay.io/keycloak/keycloak:26.6.3 start-dev

2. if it doesn't exist already, create a realm named "inventory-realm"

3. create a client called "inventory-client"
    3.1. Valid redirect URIs: http://localhost:5167/authentication/login-callback
    3.2. Valid post logout redirect URIs: http://localhost:5167/authentication/logout-callback
    3.3. Web Origins: http://localhost:5167
    3.4. Client Authentication: OFF
    3.5. Standard flow: YES (everything else from auth flow is NO)
    3.6. Require PKCE: ON - PKCE Method: S256
    3.7. Front Channel Logout: YES

4. for a page to need authentication, it needs to have the "@attribute [Authorize]" above. 

it also needs to be added to App.razor. otherwise, the information in the page will not load.

# How to run Scalar for api documentation
1. run server
2. on the url: http://localhost:5211/scalar

that's it