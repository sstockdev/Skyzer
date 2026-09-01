# Skyzer

Skyzer was created with the goal of utilizing machine learning algorithms to predict which items to buy and re-sell for a profit in Hypixel Studio's Skyblock. 

For this to work we need data to train with, that is where `Skyzer.Sync` comes in. This worker reads the available data from Hypixel's public API, aggregates that data, and stores it in a MongoDB database.

Once we have data, we want to visualize that data. That is where `Skyzer.Dashboard` comes in. `Skyzer.Dashboard` is a .NET Blazor project that was created with the goal of visualizing the MongoDB database.
