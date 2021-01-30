//MCCScript 1.0

string[] commands = new[] {
    "send command1",
    "send command2",
    "send command3"
};

int randomIndex = new Random().Next(0, commands.Length);
string randomCommand = commands[randomIndex];
MCC.PerformInternalCommand(randomCommand);