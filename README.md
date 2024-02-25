# MTCG Documentation, Tracking and Lessons Learned

## Link to Github
https://github.com/ZiraelMon/MTCG

## Mandatory Unique Feature
I decided to implement an Elemental Surge as an unique feature.
At the start of the battle, the code randomly selects a round number between 1 and 100 (elementalSurgeRound) and a random element from the ElementCard enum (surgeElement). This sets up a specific round during the battle where the Elemental Surge will take place.

During each round of the battle, the code checks if the current round number matches the previously determined elementalSurgeRound. If it does, an "Elemental Surge" occurs. This is indicated by adding a message to BattleStatistics that announces the Elemental Surge and specifies which element (surgeElement) gets a damage boost for that round.

If the Elemental Surge is active for the round, the code checks the element of the cards played by both players in that round. If a player's card matches the surgeElement, its damage is temporarily boosted by 20%, but only for that round.

## Unit Testing
User: Tests whether an incomplete request properly returns an error response, while a complete request does not. Additionally, it examines the functionality of authorization, ensuring errors are correctly returned when necessary, and successful authorization does not produce errors.

Cards: Assesses whether creating a package with an incorrect size appropriately generates an error and confirms that a correctly sized package does not cause an error. It also checks whether a card's name is accurately converted into its Type and Element, and whether an incorrect name produces the expected parsing error.

Carddeck: Verifies that updating a deck with an incorrect size correctly triggers an error response, while a properly sized update does not result in an error.

Battle: Evaluates damage calculation functions and special interactions between cards, including the dynamics of changing and removing cards during a battle.

## Design
The design is kept very simple and is mainly focused on the curl script. The decision for the simple design is that it should save time, while the logic, database and server connection were more essential for the project.

## Tracking
I have to admit that I neglected tracking a bit and sometimes worked on the code spontaneously, so I didn't track myself regularly. Unfortunately, I also created the repository on Github far too late. But I would say that I worked ~90 hours on the project. It also took me a while to get used to Visual Studio and C# and to understand the specification/requirements of the project to draw a draft and start with the structure in VS.

## Lessons Learned
I should have started sooner with the Unittests, as recommended in the lessons. The problem was that I was not yet familiar with Unit-Testing and it was therefore all the more difficult to recognize which ones were essential for my code. I also kept changing my methods, so that I would have had to adapt the test cases again and again. 
Someone who knows a lot more about coding will have a much better feel for structuring methods and recognizing more quickly what they should do and which test cases make sense.

As someone with no previous experience in coding, I still find it very difficult to come up with a good plan and structure in advance, which I should later adapt to my project. With this project I have definitely learned a lot more, but I wouldn't say I feel comfortable. The difficulty of the project was very high for me.
This is one of the reasons why I forgot about other aspects, such as better project management and time tracking, because the focus was much more on coding, getting to know the specification and becoming familiar with the IDE.

Error handling with good exceptions definitely needs to be expanded. I should certainly have included better edge cases. I will take this with me for future projects, but when it comes to implementation, it is also important how much knowledge and time is available.

