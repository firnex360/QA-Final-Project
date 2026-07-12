# QA-Final-Project

This is a repo for version control of the final project of the class "Quality Assurance"



For the project we would be using [.NET Blazor](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor).



.NET version: 10.0.300 download [here](https://dotnet.microsoft.com/es-es/download)



Intro to Blazor: [here](https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/intro)

How to run app locally once clone the project: [here](https://dotnet.microsoft.com/en-us/learn/aspnet/blazor-tutorial/run) (this is just for Blazor apps)



# How to run project on Docker Compose!!!

Can boot up the project through doker compose.



1. on "/src" docker compose up --build
2. make sure to update the database again. delete all volume related to DB in docker and update it (dotnet ef database update)



That's it!1

oh and to reset visuals (to avoid always running the whole docker)
docker compose up -d --build client

and for the bd when it needs a reset
dotnet ef database update



# How to run the actual project (without Docker Compose)

1. Open the folder "InventoryManagement". In here, 3 project will load under the folder "src".
2. Open terminal, enter the folder of "src\\InventoryManagement.Server" and run dotnet watch
3. Open another terminal, enter the folder of "src\\InventoryManagement.Client" and run dotnet watch. This should open the visual of the app



# How to run database postgres(without Docker Compose)

1. Run this on terminal: docker run --name inventory -e POSTGRES\_USER=admin -e POSTGRES\_PASSWORD=password -e POSTGRES\_DB=InventoryDb -p 5454:5432 -d postgres:18.4
2. if any errors occur that refer directly to postgres, run this inside inventorysystem.server to make sure the database is up to date: dotnet ef database update
3. if any errors occur that refer directly to postgres, run this inside inventorysystem.server to make sure the database is up to date: dotnet ef database update

that's it. posqgres will be running on docker and can be access through the connection made in appsetting.json on "ConnectionStrings" part



# How to create authentication pass

1. run the following (if not using docker compose): docker run -p 127.0.0.1:8080:8080 -e KC\_BOOTSTRAP\_ADMIN\_USERNAME=admin -e KC\_BOOTSTRAP\_ADMIN\_PASSWORD=admin quay.io/keycloak/keycloak:26.6.3 start-dev
2. if it doesn't exist already, create a realm named "inventory-realm"
3. create a client called "inventory-client"

   1. &#x20;Client Authentication: OFF
   2. Standard flow: YES (everything else from auth flow is NO)
   3. Require PKCE: ON - PKCE Method: S256
   4. Valid redirect URIs: http://localhost:9090/authentication/login-callback
   5. Valid post logout redirect URIs: http://localhost:9090/authentication/logout-callback
   6. Web Origins: http://localhost:9090
   7. Front Channel Logout: YES



For a page to need authentication, it needs to have the "@attribute \[Authorize]" above.

it also needs to be added to App.razor. otherwise, the information in the page will not load.



# How to run Scalar for api documentation

1. run server
2. on the url: http://localhost:8090/scalar

that's it



# How to create Policies (before I forget how to do it)



1. Go to keycloak (http://localhost:8080)
2. CREATE ROLES -- Go into Clients > inventory-client > Roles. Here, you will create all the roles designated for your project.
3. CREATE MAPPER -- Go into Clients > inventory-client > Client Scopes > inventory-client-dedicated > Create a new mapper (if no mappers yet) and make sure it has the following settings:
SELECT -- Mapper type: User Client Role
Client ID: inventory-client
Token Claim Name: roles
Claim JSON Type: String
Add to access token: ON
Add to ID token: ON
Add to userinfo: ON
4. CREATE USER -- Go to Users > Add a username \& email > Save > Go to credentials > Set Password \& turn temporary OFF
5. ADD ROLE TO USER -- Go to Users > Select the desired user > Role Mapping > Assign Role \*(CLIENT ROLE) > Select desired role \& Apply.

Code-wise

1. This first version handles the APIs directly, so each API has the \[Authorize(Policy = "CanXXX")] above, which is how we can call that policy later on.
2. In the client \[Product.cs], inside the AddAuthorizationCore we can add the Policy name and the roles that can use those policies. Same goes for server \[Product.cs], inside AddAuthorization.

