using System;

namespace MinecraftClient.CommandHandler
{
    public class CmdResult
    {
        public static readonly CmdResult Empty = new();

        internal static McClient? currentHandler;

        public enum Status
        {
            NotRun = int.MinValue,
            FailChunkNotLoad = -4,
            FailNeedEntity = -3,
            FailNeedInventory = -2,
            FailNeedTerrain = -1,
            Fail = 0,
            Done = 1,
        }

        public CmdResult()
        {
            this.status = Status.NotRun;
            this.result = null;
        }

        public Status status;

        public string? result;

        public int SetAndReturn(Status status)
        {
            this.status = status;
            this.result = status switch
            {
#pragma warning disable format // @formatter:off
                Status.NotRun             =>  null,
                Status.FailChunkNotLoad   =>  null,
                Status.FailNeedEntity     =>  Translations.extra_entity_required,
                Status.FailNeedInventory  =>  Translations.extra_inventory_required,
                Status.FailNeedTerrain    =>  Translations.extra_terrainandmovement_required,
                Status.Fail               =>  Translations.general_fail,
                Status.Done               =>  null,
                _                         =>  null,
#pragma warning restore format // @formatter:on
            };
            return Convert.ToInt32(this.status);
        }

        public int SetAndReturn(Status status, string? result)
        {
            this.status = status;
            this.result = result;
            return Convert.ToInt32(this.status);
        }

        public int SetAndReturn(int code, string? result)
        {
            this.status = (Status)Enum.ToObject(typeof(Status), code);
            if (!Enum.IsDefined(typeof(Status), status))
                throw new InvalidOperationException($"{code} is not a legal return value.");
            this.result = result;
            return code;
        }

        public int SetAndReturn(bool result)
        {
            this.status = result ? Status.Done : Status.Fail;
            this.result = result ? Translations.general_done : Translations.general_fail;
            return Convert.ToInt32(this.status);
        }

        public int SetAndReturn(string? result)
        {
            this.status = Status.Done;
            this.result = result;
            return Convert.ToInt32(this.status);
        }

        public int SetAndReturn(string? resultstr, bool result)
        {
            this.status = result ? Status.Done : Status.Fail;
            this.result = resultstr;
            return Convert.ToInt32(this.status);
        }

        public override string ToString()
        {
            if (result != null)
                return result;
            else
                return status.ToString();
        }
    }
}
