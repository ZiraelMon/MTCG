using Moq;
using MTCG.Models;
using MTCG.Server;
using Npgsql;
using System.Text.Json;


namespace MTCGTesting {

    [TestFixture]

public class UserTests {
        private User _user;
        private Token _userToken;
        private Mock<HttpSvrEventArgs> _mockEvent;
        private Mock<NpgsqlConnection> _mockConnection;
        private Mock<Token> _mockToken;

        [SetUp]
        public void Setup() {
            _user = new User();
            _userToken = new Token();
            _mockEvent = new Mock<HttpSvrEventArgs>();
            _mockConnection = new Mock<NpgsqlConnection>();
            _mockToken = new Mock<Token>();
        }

        [Test]
        public void CreateUser_WithValidData_ShouldCreateUser() {
            // Arrange
            var payload = JsonSerializer.Serialize(new User { Username = "test", Password = "password" });
            _mockEvent.Setup(e => e.Payload).Returns(payload);

            // Mock the database connection and transaction
            var mockTransaction = new Mock<NpgsqlTransaction>();
            _mockConnection.Setup(c => c.BeginTransaction()).Returns(mockTransaction.Object);

            // Act
            _user.CreateUser(_mockEvent.Object);

            // Assert
            // Verify that the appropriate methods were called on _mockEvent and _mockConnection
            _mockEvent.Verify(e => e.Reply(200, "User created successfully"), Times.Once);
            _mockConnection.Verify(c => c.BeginTransaction(), Times.Once);
            mockTransaction.Verify(t => t.Commit(), Times.Once);
        }

        [Test]
        public void CreateUser_WithInvalidData_ShouldReturnError() {
            // Arrange
            var payload = JsonSerializer.Serialize(new User { Username = "", Password = "" });
            _mockEvent.Setup(e => e.Payload).Returns(payload);

            // Act
            _user.CreateUser(_mockEvent.Object);

            // Assert
            _mockEvent.Verify(e => e.Reply(400, "Error: Username or password cannot be empty."), Times.Once);
        }

        [Test]
        public void UpdateUserData_SuccessfulUpdate() {
            // Arrange
            _mockEvent.Setup(e => e.Path).Returns("/user/testuser");
            _mockEvent.Setup(e => e.Payload).Returns("{\"Name\":\"New Name\",\"Info\":\"New Info\",\"Image\":\"New Image\"}");
            _mockToken.Setup(t => t.LoggedInUser).Returns("testuser");

            // Act
            _user.UpdateUserData(_mockEvent.Object, _mockToken.Object);

            // Assert
            _mockEvent.Verify(e => e.Reply(200, "Profile update successful."), Times.Once);
        }

        [Test]
        public void UpdateUserData_UnauthorizedUpdate() {
            // Arrange
            _mockEvent.Setup(e => e.Path).Returns("/user/testuser");
            _mockEvent.Setup(e => e.Payload).Returns("{\"Name\":\"New Name\",\"Info\":\"New Info\",\"Image\":\"New Image\"}");
            _mockToken.Setup(t => t.LoggedInUser).Returns("wronguser");

            // Act
            _user.UpdateUserData(_mockEvent.Object, _mockToken.Object);

            // Assert
            _mockEvent.Verify(e => e.Reply(400, "Authorization doesn't match request."), Times.Once);
        }

        [Test, Order(1)]
        public void CreateUser_FailureEmptyPassword() {
            _mockEvent.Setup(e => e.Payload).Returns("{\"Username\":\"username\", \"Password\":\"\"}");
            _user.CreateUser(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error occured while creating user."));
        }

        [Test, Order(2)]
        public void CreateUser_FailureEmptyUsername() {
            _mockEvent.Setup(e => e.Payload).Returns("{\"Username\":\"username\", \"Password\":\"\"}");
            _user.CreateUser(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error occured while creating user."));
        }

        // reply should be 400 if the username provided in the path doesnt match the token
        [Test, Order(3)]
        public void UpdateUserData_AuthorizationError() {
            _mockEvent.Setup(e => e.Path).Returns("/users/kienboec");
            _userToken.LoggedInUser = "name";
            _user.UpdateUserData(_mockEvent.Object, _userToken);
            _mockEvent.Verify(e => e.Reply(400, "Authorization doesn't match request."));
        }

        [Test, Order(4)]
        // reply should not be an authorization error if the names match
        public void UpdateUserData_NoAuthorizationError() {
            var mockEventArgs = new Mock<HttpSvrEventArgs>();
            _mockEvent.Setup(e => e.Path).Returns("/users/name");
            _userToken.LoggedInUser = "name";
            _user.UpdateUserData(_mockEvent.Object, _userToken);
            _mockEvent.Verify(e => e.Reply(400, "Authorization doesn't match request."), Times.Never);
        }
    }

public class CardTests {

        private Mock<HttpSvrEventArgs> _mockEvent;

        [SetUp]
        public void SetUp() {
            _mockEvent = new Mock<HttpSvrEventArgs>();
        }

        [Test]
        public void CreateCards_FailureSize() {
            _mockEvent.Setup(e => e.Payload).Returns(
                "[{\"Id\":\"845f0dc7-37d0-426e-994e-43fc3ac83c08\", \"Name\":\"WaterGoblin\", \"Damage\": 10.0}, " +
                "{\"Id\":\"99f8f8dc-e25e-4a95-aa2c-782823f36e2a\", \"Name\":\"Dragon\", \"Damage\": 50.0}, " +
                "{\"Id\":\"e85e3976-7c86-4d06-9a80-641c2019a79f\", \"Name\":\"WaterSpell\", \"Damage\": 20.0}, " +
                "{\"Id\":\"1cb6ab86-bdb2-47e5-b6e4-68c5ab389334\", \"Name\":\"Ork\", \"Damage\": 45.0}]");
            var card = new Card();
            card.CreateCards(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error occured while creating package: not enough cards for a package."));
        }

        [Test]
        public void CreateCards_SuccessSize() {
            _mockEvent.Setup(e => e.Payload).Returns(
                "[{\"Id\":\"845f0dc7-37d0-426e-994e-43fc3ac83c08\", \"Name\":\"WaterGoblin\", \"Damage\": 10.0}, " +
                "{\"Id\":\"99f8f8dc-e25e-4a95-aa2c-782823f36e2a\", \"Name\":\"Dragon\", \"Damage\": 50.0}, " +
                "{\"Id\":\"e85e3976-7c86-4d06-9a80-641c2019a79f\", \"Name\":\"WaterSpell\", \"Damage\": 20.0}, " +
                "{\"Id\":\"1cb6ab86-bdb2-47e5-b6e4-68c5ab389334\", \"Name\":\"Ork\", \"Damage\": 45.0}]" +
                "{\"Id\":\"9e8238a4-8a7a-487f-9f7d-a8c97899eb48\", \"Name\":\"Dragon\", \"Damage\": 70.0},]");
                var card = new Card();
            card.CreateCards(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error occured while creating package: not enough cards for a package."), Times.Never());
        }

        [Test]
        public void GetCardStats_SuccessSpell() {
            Card card = new Card();
            card.Name = "FireSpell";
            card.GetCardStats(card);
            Assert.That(card.Element, Is.EqualTo(Card.ElementCard.Fire));
            Assert.That(card.Species, Is.EqualTo(Card.SpeciesCard.Spell));
        }

        [Test]
        public void GetCardStats_SuccessMonster() {
            Card card2 = new Card();
            card2.Name = "Kraken";
            card2.GetCardStats(card2);
            Assert.That(card2.Element, Is.EqualTo(Card.ElementCard.Regular));
            Assert.That(card2.Species, Is.EqualTo(Card.SpeciesCard.Kraken));
        }

        [Test]
        public void GetCardStatsFailure() {
            Card card = new Card();
            card.Name = "DarkSpell";

            Assert.That(() => card.GetCardStats(card), Throws.TypeOf<ArgumentException>());
        }
    }

public class CardCollectionTests {

        private Mock<HttpSvrEventArgs> _mockEvent;
        private Mock<Token> _mockToken;
        private Deck _deck;

        [SetUp]
        public void SetUp() {
            _mockEvent = new Mock<HttpSvrEventArgs>();
            _mockToken = new Mock<Token>();
            _deck = new Deck();
        }

        [Test]
        public void UpdateDeck_FailureSize() {
            _mockEvent.Setup(e => e.Payload).Returns("[\"aa9999a0-734c-49c6-8f4a-651864b14e62\", \"d6e9c720-9b5a-40c7-a6b2-bc34752e3463\", \"d60e23cf-2238-4d49-844f-c7589ee5342e\"]");
            _deck.UpdateDeck(_mockEvent.Object, _mockToken.Object);
            _mockEvent.Verify(e => e.Reply(400, "Malformed Request to update Decks."));
        }

        [Test]
            public void UpdateDeck_CorrectSize() {
            _mockEvent.Setup(e => e.Payload).Returns("\"[\\\"aa9999a0-734c-49c6-8f4a-651864b14e62\\\", \\\"d6e9c720-9b5a-40c7-a6b2-bc34752e3463\\\", \\\"d60e23cf-2238-4d49-844f-c7589ee5342e\\\", \\\"845f0dc7-37d0-426e-994e-43fc3ac83c08\\\"]\"");
            _deck.UpdateDeck(_mockEvent.Object, _mockToken.Object);
            _mockEvent.Verify(e => e.Reply(400, "Malformed Request to update Decks."));
        }
    }

public class BattleTests {
        private Mock<Token> _mockToken1;
        private Mock<Token> _mockToken2;
        private Battle _battle;
        private Card _PlayerOneCard;
        private Card _PlayerTwoCard;

        [SetUp]
        public void SetUp() {
            _mockToken1 = new Mock<Token>();
            _mockToken2 = new Mock<Token>();
            _battle = new Battle(_mockToken1.Object, _mockToken2.Object);
            _PlayerOneCard = new Card();
            _PlayerTwoCard = new Card();
        }

        [Test]
        public void CardInteraction_p1DragonDefeatsGoblin() {
            _PlayerOneCard.Species = Card.SpeciesCard.Dragon;
            _PlayerTwoCard.Species = Card.SpeciesCard.Goblin;

            string result = _battle.CardInteraction(_PlayerOneCard, _PlayerTwoCard);
            Assert.That(result, Is.EqualTo("Dragon defeats Goblin\n"));
            Assert.That(_battle.PlayerOneWon, Is.EqualTo(true));
        }

        [Test]
        public void CardInteraction_p2WaterSpellDrownsKnight() {
            _PlayerOneCard.Species = Card.SpeciesCard.Knight;
            _PlayerTwoCard.Species = Card.SpeciesCard.Spell;
            _PlayerTwoCard.Element = Card.ElementCard.Water;

            string result = _battle.CardInteraction(_PlayerOneCard, _PlayerTwoCard);
            Assert.That(result, Is.EqualTo("WaterSpell drowns Knight\n"));
            Assert.That(_battle.PlayerOneWon, Is.EqualTo(false));
        }

        [Test]
        public void CardInteraction_OrkVsOrk() {
            _PlayerOneCard.Species = Card.SpeciesCard.Ork;
            _PlayerTwoCard.Species = Card.SpeciesCard.Ork;

            string result = _battle.CardInteraction(_PlayerOneCard, _PlayerTwoCard);
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public void ChangeCard_p1SuccessfulCardTransfer() {

            _battle.PlayerOneDeck.Add(_PlayerOneCard);
            _battle.PlayerTwoDeck.Add(_PlayerTwoCard);
            _battle.PlayerOneWon = true;

            _battle.ChangeCard(0, 0);

            Assert.That(_battle.PlayerOneDeck.Count(), Is.EqualTo(2));
            Assert.That(_battle.PlayerTwoDeck.Count(), Is.EqualTo(0));
        }

        [Test]
        public void ChangeCard_p2SuccessfulCardTransfer() {

            _battle.PlayerOneDeck.Add(_PlayerOneCard);
            _battle.PlayerTwoDeck.Add(_PlayerTwoCard);
            _battle.PlayerOneWon = false;

            _battle.ChangeCard(0, 0);

            Assert.That(_battle.PlayerOneDeck.Count(), Is.EqualTo(0));
            Assert.That(_battle.PlayerTwoDeck.Count(), Is.EqualTo(2));
        }

        [Test]
        public void TotalDamage_WaterSpellVsFireSpell() {
            _PlayerOneCard.Name = "WaterSpell";
            _PlayerOneCard.Damage = 20.0;

            _PlayerTwoCard.Name = "FireSpell";
            _PlayerTwoCard.Damage = 20.0;

            double[] totalDamage = _battle.TotalDamage(_PlayerOneCard, _PlayerTwoCard);

            Assert.That(totalDamage[0], Is.EqualTo(40.0));
            Assert.That(totalDamage[1], Is.EqualTo(10.0));
        }

        [Test]
        public void TotalDamage_WaterGoblinVsFireGoblin() {
            _PlayerOneCard.Name = "WaterGoblin";
            _PlayerOneCard.Damage = 20.0;

            _PlayerTwoCard.Name = "FireGoblin";
            _PlayerTwoCard.Damage = 20.0;

            double[] totalDamage = _battle.TotalDamage(_PlayerOneCard, _PlayerTwoCard);

            Assert.That(totalDamage[0], Is.EqualTo(20.0));
            Assert.That(totalDamage[1], Is.EqualTo(20.0));
        }

        [Test]
        public void TotalDamage_WaterGoblinVsFireSpell() {
            _PlayerOneCard.Name = "WaterGoblin";
            _PlayerOneCard.Damage = 20.0;

            _PlayerTwoCard.Name = "FireSpell";
            _PlayerTwoCard.Damage = 20.0;

            double[] totalDamage = _battle.TotalDamage(_PlayerOneCard, _PlayerTwoCard);

            Assert.That(totalDamage[0], Is.EqualTo(40.0));
            Assert.That(totalDamage[1], Is.EqualTo(10.0));
        }

        [Test]
        public void TotalDamage_WaterGoblinVsWaterSpell() {
            _PlayerOneCard.Name = "WaterGoblin";
            _PlayerOneCard.Damage = 20.0;

            _PlayerTwoCard.Name = "WaterSpell";
            _PlayerTwoCard.Damage = 20.0;

            double[] totalDamage = _battle.TotalDamage(_PlayerOneCard, _PlayerTwoCard);

            Assert.That(totalDamage[0], Is.EqualTo(20.0));
            Assert.That(totalDamage[1], Is.EqualTo(20.0));
        }

        [Test]
        public void RemoveCard_p1Wins() {
            _PlayerOneCard.Damage = 20.0;
            _PlayerTwoCard.Damage = 10.0;

            _battle.PlayerOneDeck.Add(_PlayerOneCard);
            _battle.PlayerTwoDeck.Add(_PlayerTwoCard);
            _battle.PlayerOneWon = true;

            _battle.RemoveCard(0, 0);

            Assert.That(_battle.PlayerOneDeck.Count(), Is.EqualTo(1));
            Assert.That(_battle.PlayerOneDeck[0].Damage, Is.EqualTo(31.0));
            Assert.That(_battle.PlayerTwoDeck.Count(), Is.EqualTo(0));
        }

        [Test]
        public void RemoveCard_p2Wins() {
            _PlayerOneCard.Damage = 20.0;
            _PlayerTwoCard.Damage = 10.0;

            _battle.PlayerOneDeck.Add(_PlayerOneCard);
            _battle.PlayerTwoDeck.Add(_PlayerTwoCard);
            _battle.PlayerOneWon = false;

            _battle.RemoveCard(0, 0);

            Assert.That(_battle.PlayerTwoDeck.Count(), Is.EqualTo(1));
            Assert.That(_battle.PlayerTwoDeck[0].Damage, Is.EqualTo(21.0));
            Assert.That(_battle.PlayerOneDeck.Count(), Is.EqualTo(0));
        }
    }
}