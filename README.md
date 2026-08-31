# Skyzer

Skyzer was created with the goal of utilizing machine learning algorithms to predict which items to buy and re-sell for a profit in Hypixel Studio's Skyblock. 

For this to work we need data to train with, that is where `Skyzer.Sync` comes in. This worker reads the available data from Hypixel's public API, aggregates that data, and stores it in a MongoDB database.

WOnce we have data, we need to read that data. That is where `Skyzer.Api` comes in. `Skyzer.Api` is an ASP .NET MVC project that was created with the goal of facilitating CRUD operations on the MongoDB database containing auction information and also as a way to run certain operations on that data.