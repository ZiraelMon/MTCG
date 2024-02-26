using Moq;
using MTCG.Models;
using MTCG.Server;
using Newtonsoft.Json.Linq;
using Npgsql;
using System.Text.Json;


namespace MTCGTesting {

    [TestFixture]

public class UserTests {
        private User _user;
        private Token _userToken;
        private Mock<HttpSvrEventArgs> _mockEvent;

        [SetUp]
        public void Setup() {
            _user = new User();
            _userToken = new Token();
            _mockEvent = new Mock<HttpSvrEventArgs>();
        }

        [Test]
        public void CreateUser_FailureEmptyPassword() {
            _mockEvent.Setup(e => e.Payload).Returns("{\"Username\":\"username\", \"Password\":\"\"}");
            _user.CreateUser(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error: Username or password cannot be empty."));
        }

        [Test]
        public void CreateUser_FailureEmptyUsername() {
            _mockEvent.Setup(e => e.Payload).Returns("{\"Username\":\"username\", \"Password\":\"\"}");
            _user.CreateUser(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error: Username or password cannot be empty."));
        }

        [Test]
        public void UpdateUserData_AuthorizationError() {
            _mockEvent.Setup(e => e.Path).Returns("/users/kienboec");
            _userToken.LoggedInUser = "name";
            _user.UpdateUserData(_mockEvent.Object, _userToken);
            _mockEvent.Verify(e => e.Reply(400, "Authorization does not match request."));
        }

        [Test]
        public void UpdateUserData_NoAuthorizationError() {
            _mockEvent.Setup(e => e.Path).Returns("/users/name");
            _userToken.LoggedInUser = "name";
            _user.UpdateUserData(_mockEvent.Object, _userToken);
            _mockEvent.Verify(e => e.Reply(400, "Authorization does not match request."), Times.Never);
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
            // Setup the payload with only 4 Card objects to simulate the failure condition.
            _mockEvent.Setup(e => e.Payload).Returns(
                "[{\"Id\":\"845f0dc7-37d0-426e-994e-43fc3ac83c08\", \"Name\":\"WaterGoblin\", \"Damage\": 10.0}, " +
                "{\"Id\":\"99f8f8dc-e25e-4a95-aa2c-782823f36e2a\", \"Name\":\"Dragon\", \"Damage\": 50.0}, " +
                "{\"Id\":\"e85e3976-7c86-4d06-9a80-641c2019a79f\", \"Name\":\"WaterSpell\", \"Damage\": 20.0}, " +
                "{\"Id\":\"1cb6ab86-bdb2-47e5-b6e4-68c5ab389334\", \"Name\":\"Ork\", \"Damage\": 45.0}]");

            var card = new Card();
            card.CreateCards(_mockEvent.Object);
            _mockEvent.Verify(e => e.Reply(400, "Error occurred while creating package: not enough cards for a package."), Times.Once());
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

    public class DeckTests {
        private Mock<HttpSvrEventArgs> _mockEvent;
        private Token _token;
        private Deck _deck;

        [SetUp]
        public void SetUp() {
            _mockEvent = new Mock<HttpSvrEventArgs>();
            _deck = new Deck();
            _token = new Token();
        }

        [Test]
        public void UpdateDeck_WithValidTokenAndCorrectPayload_ShouldAttemptUpdate() {
            var guidArray = "[\"aa9999a0-734c-49c6-8f4a-651864b14e62\", \"d6e9c720-9b5a-40c7-a6b2-bc34752e3463\", \"d60e23cf-2238-4d49-844f-c7589ee5342e\", \"845f0dc7-37d0-426e-994e-43fc3ac83c08\"]";
            _mockEvent.Setup(e => e.Payload).Returns(guidArray);
            _deck.UpdateDeck(_mockEvent.Object, _token);
            _mockEvent.Verify(e => e.Reply(It.IsAny<int>(), It.IsAny<string>()), Times.Once);

        }

        [Test]
        public void UpdateDeck_WithValidTokenAndIncorrectPayload_ShouldFailToUpdate() {
            var incorrectGuidArray = "[\"aa9999a0-734c-49c6-8f4a-651864b14e62\", \"d6e9c720-9b5a-40c7-a6b2-bc34752e3463\"]"; // Only 2 GUIDs, expecting 4
            _mockEvent.Setup(e => e.Payload).Returns(incorrectGuidArray);
            _deck.UpdateDeck(_mockEvent.Object, _token);
            _mockEvent.Verify(e => e.Reply(It.IsAny<int>(), It.IsAny<string>()), Times.Once);
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
            Assert.That(_battle.PlayerOneDeck[0].Damage, Is.EqualTo(24.0));
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
            Assert.That(_battle.PlayerTwoDeck[0].Damage, Is.EqualTo(12.0));
            Assert.That(_battle.PlayerOneDeck.Count(), Is.EqualTo(0));
        }
    }
}