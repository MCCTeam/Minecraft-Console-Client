//MCCScript 1.0

MCC.LoadBot(new AutoLeaveOnLowHp());

//MCCScript Extensions

namespace MinecraftClient.ChatBots
{
    public class AutoLeaveOnLowHp : ChatBot
    {
        private float HEALTH_BOUNDARY = 10.0f; // 10 HP

        public override void OnHealthUpdate(float health, int food)
        {
            if (health <= HEALTH_BOUNDARY)
            {
                LogToConsole("Leaving because of low HP (Reconnecting in 5 minutes)!");
                ReconnectToTheServer(-1, 300); // Disconnect and reconnect after 5 minutes (300 seconds)
            }
        }
    }
}
