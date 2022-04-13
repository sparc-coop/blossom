## Connect your UI to your Features

1. Right-click your Web Project -> Add Connected Service -> Add Service Reference -> OpenAPI
2. Choose the "File" Radio button, and navigate and select the `swagger.json` file generated inside your Features Project.
3. Type in any namespace and class name you desire for your client-side Api class.
4. Click OK. The Api class will now be generated for you, and will regenerate automatically on every new build.
	> If you ever need to manually regenerate this class, simply open the Connected Service -> Regenerate.

5. Inject the Api class into your app (preferably in the `_Imports.razor` global file):
	```razor
	@inject PointOfSaleApi Api
```
6. Use the Api class throughout your application.
	```razor
	var orders = await Api.GetAllOrdersAsync();
	```

