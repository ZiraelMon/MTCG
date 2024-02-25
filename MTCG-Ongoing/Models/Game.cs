using MTCG;
using MTCG.Controller;
using MTCG.Server;
using System.Collections.Concurrent;
using System.Numerics;
using System.Threading;

namespace MTCG.Models {
    public static class Game {
        private static ConcurrentQueue<Token> PlayerQueue = new ConcurrentQueue<Token>();

        private static SemaphoreSlim Semaphore = new SemaphoreSlim(1, 1);

        private static ConcurrentDictionary<Token, string> BattleStatistics = new ConcurrentDictionary<Token, string>();

        public static void Join(HttpSvrEventArgs e, Token player) {
            try {
                Token? playerOne = null, playerTwo = null;

                Semaphore.Wait();

                try {
                    PlayerQueue.Enqueue(player);

                    if (PlayerQueue.Count >= 2) {
                        PlayerQueue.TryDequeue(out playerOne);
                        PlayerQueue.TryDequeue(out playerTwo);
                        if (playerOne == playerTwo) {
                            e.Reply(400, "Cannot battle yourself.");
                            return;
                        }
                    }
                }
                finally {
                    Semaphore.Release();
                }

                if ((playerOne != null) && (playerTwo != null)) {
                    var battle = new Battle(playerOne, playerTwo);
                    string result = battle.StartBattle();
                    BattleStatistics.TryAdd(playerOne, result);
                    e.Reply(200, result);
                    return;
                }

                int attempt = 0;
                while (!BattleStatistics.ContainsKey(player)) {
                    Thread.Sleep(500); // Wait for half a second
                    attempt++;
                    if (attempt >= 20) {
                        e.Reply(400, "No Battle found - please queue again.");
                        return;
                    }
                }
                if (BattleStatistics.TryRemove(player, out string? battleStatistics)) {
                    // Successfully removed the battle Statistics
                    e.Reply(200, battleStatistics);
                }
                else {
                    e.Reply(400, "Could not find or remove the battle Statistics.");
                }
            }
            catch (Exception ex) {
                Console.WriteLine(ex);
                e.Reply(400, "Error with Battle/Request.");
            }
        }

    }
}