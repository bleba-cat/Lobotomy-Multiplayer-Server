using LiteNetLib.Utils;
using System.Text;

namespace NetworkServer.PacketConvertation
{
    public static class SerializePacket
    {
        public static void Message(NetDataWriter writer, string message)
        {
            writer.Put((int)PacketType.Message);
            writer.Put(message);
        }
        public static (byte[], int) MessageAsBytes(ReadOnlySpan<char> message)
        {
            byte[] result = new byte[50];
            result[0] = (int)PacketType.Message;
            int msgLength = Encoding.UTF8.GetByteCount(message);
            result[1] = (byte)(msgLength >> 24);
            result[2] = (byte)(msgLength >> 16);
            result[3] = (byte)(msgLength >> 8);
            result[4] = (byte)msgLength;
            Encoding.UTF8.GetBytes(message, result.AsSpan().Slice(5));
            return (result, 5 + msgLength);
        }
        public static void ChatMessage(NetDataWriter writer, string sender, string message)
        {
            writer.Put((int)PacketType.ChatMessage);
            writer.Put(sender);
            writer.Put(message);
        }
        public static void PlayerList(NetDataWriter writer, ICollection<Player> players)
        {
            writer.Put((int)PacketType.PlayerList);
            writer.Put((byte)players.Count);
            foreach (Player player in players)
            {
                writer.Put(player.Nickname);
                writer.Put(player.IsHost);
                writer.Put(player.IsConfirmed);
                writer.Put(player.Permissions.perm1);
            }
        }
        public static void RoomCode(NetDataWriter writer, string code)
        {
            writer.Put((int)PacketType.RoomCode);
            writer.Put(code);
        }
        public static void ConfirmUser(NetDataWriter writer, string nickname)
        {
            writer.Put((int)PacketType.ConfirmUser);
            writer.Put(nickname);
        }
        public static void KickUser(NetDataWriter writer, string nickname)
        {
            writer.Put((int)PacketType.KickUser);
            writer.Put(nickname);
        }
        public static void Save(NetDataWriter writer, byte[] save)
        {
            writer.Put((int)PacketType.Save);
            for (int i = 0; i < save.Length; i++)
            {
                writer.Put(save[i]);
            }
        }
        /*public static (byte[], int) NewAgent(AgentData data, WorkerPrimaryStat stats, int prefixId, int suffixId, int cost)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.NewAgent);
            writer.WriteBool(data.isCustomName);
            if (data.isCustomName) writer.WriteString(data.CustomName);
            else writer.WriteInt(data.agentName.id);
            writer.WriteLong(data.appearance.spriteSet.saveData.Eye.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.Eye.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.EyeClose.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.EyeClose.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.EyePanic.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.EyePanic.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.EyeDead.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.EyeDead.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.EyeBrow.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.EyeBrow.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.BattleEyeBrow.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.BattleEyeBrow.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.PanicEyeBrow.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.PanicEyeBrow.b);
            writer.WriteFloat(data.appearance.spriteSet.saveData.EyeColor.r);
            writer.WriteFloat(data.appearance.spriteSet.saveData.EyeColor.g);
            writer.WriteFloat(data.appearance.spriteSet.saveData.EyeColor.b);
            writer.WriteFloat(data.appearance.spriteSet.saveData.EyeColor.a);
            writer.WriteLong(data.appearance.spriteSet.saveData.Mouth.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.Mouth.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.BattleMouth.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.BattleMouth.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.PanicMouth.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.PanicMouth.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.FrontHair.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.FrontHair.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.RearHair.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.RearHair.b);
            writer.WriteLong(data.appearance.spriteSet.saveData.AttachmentHair.a);
            writer.WriteInt(data.appearance.spriteSet.saveData.AttachmentHair.b);
            writer.WriteFloat(data.appearance.spriteSet.saveData.HairColor.r);
            writer.WriteFloat(data.appearance.spriteSet.saveData.HairColor.g);
            writer.WriteFloat(data.appearance.spriteSet.saveData.HairColor.b);
            writer.WriteFloat(data.appearance.spriteSet.saveData.HairColor.a);
            writer.WriteInt(stats.hp);
            writer.WriteInt(stats.mental);
            writer.WriteInt(stats.work);
            writer.WriteInt(stats.battle);
            writer.WriteInt(prefixId);
            writer.WriteInt(suffixId);
            writer.WriteInt(cost);
            return (result, writer.Position);
        }
        public static (byte[], int) ChangeAgent(AgentModel agent, WorkerPrimaryStat stats, int prefixId, int suffixId, int cost)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.ChangeAgent);
            writer.WriteLong(agent.instanceId);
            writer.WriteInt(stats.hp);
            writer.WriteInt(stats.mental);
            writer.WriteInt(stats.work);
            writer.WriteInt(stats.battle);
            writer.WriteInt(prefixId);
            writer.WriteInt(suffixId);
            writer.WriteInt(cost);
            return (result, writer.Position);
        }
        public static (byte[], int) EquipEGO(AgentModel agent, EquipmentModel equipment)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.EquipEGO);
            writer.WriteLong(agent.instanceId);
            writer.WriteLong(equipment.instanceId);
            return (result, writer.Position);
        }
        public static (byte[], int) SefiraChange(SefiraEnum sefira, AgentModel agent, MapNode node)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.SefiraChange);
            writer.WriteByte((byte)sefira);
            writer.WriteLong(agent.instanceId);
            writer.WriteMapNode(node);
            return (result, writer.Position);
        }
        public static (byte[], int) InitAgentAdd(AgentModel agent, MapNode node)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.InitAgentAdd);
            writer.WriteLong(agent.instanceId);
            writer.WriteMapNode(node);
            return (result, writer.Position);
        }
        public static (byte[], int) AgentRemove(SefiraPanel panel, int index)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.RemoveAgent);
            writer.WriteByte((byte)panel.Sefira.sefiraEnum);
            writer.WriteInt(index);
            return (result, writer.Position);
        }
        public static (byte[], int) DeployAgent(AgentModel agent)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.DeployAgent);
            writer.WriteLong(agent.instanceId);
            return (result, writer.Position);
        }
        public static (byte[], int) OfficersInit(InitOfficers officers)
        {
            byte[] result = MemoryPool.Rent(4096);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.InitOfficers);
            writer.WriteByte((byte)officers.currSefira.sefiraEnum);
            writer.WriteInt(officers.officers.Count);
            foreach (InitOfficers.OfficerData data in officers.officers)
            {
                writer.WriteLong(data.id);
                writer.WriteString(data.name);
                writer.WriteInt(data.MaxHp);
                writer.WriteInt(data.MaxMental);
                writer.WriteFloat(data.baseMovement);
                writer.WriteLong(data.setData.Eye.a);
                writer.WriteInt(data.setData.Eye.b);
                writer.WriteLong(data.setData.EyeClose.a);
                writer.WriteInt(data.setData.EyeClose.b);
                writer.WriteLong(data.setData.EyePanic.a);
                writer.WriteInt(data.setData.EyePanic.b);
                writer.WriteLong(data.setData.EyeDead.a);
                writer.WriteInt(data.setData.EyeDead.b);
                writer.WriteLong(data.setData.EyeBrow.a);
                writer.WriteInt(data.setData.EyeBrow.b);
                writer.WriteLong(data.setData.BattleEyeBrow.a);
                writer.WriteInt(data.setData.BattleEyeBrow.b);
                writer.WriteLong(data.setData.PanicEyeBrow.a);
                writer.WriteInt(data.setData.PanicEyeBrow.b);
                writer.WriteFloat(data.setData.EyeColor.r);
                writer.WriteFloat(data.setData.EyeColor.g);
                writer.WriteFloat(data.setData.EyeColor.b);
                writer.WriteFloat(data.setData.EyeColor.a);
                writer.WriteLong(data.setData.Mouth.a);
                writer.WriteInt(data.setData.Mouth.b);
                writer.WriteLong(data.setData.BattleMouth.a);
                writer.WriteInt(data.setData.BattleMouth.b);
                writer.WriteLong(data.setData.PanicMouth.a);
                writer.WriteInt(data.setData.PanicMouth.b);
                writer.WriteLong(data.setData.FrontHair.a);
                writer.WriteInt(data.setData.FrontHair.b);
                writer.WriteLong(data.setData.RearHair.a);
                writer.WriteInt(data.setData.RearHair.b);
                writer.WriteLong(data.setData.AttachmentHair.a);
                writer.WriteInt(data.setData.AttachmentHair.b);
                writer.WriteFloat(data.setData.HairColor.r);
                writer.WriteFloat(data.setData.HairColor.g);
                writer.WriteFloat(data.setData.HairColor.b);
                writer.WriteFloat(data.setData.HairColor.a);
                writer.WriteString(data.currNodeId);
                writer.WriteString(data.nodeId);
            }
            return (result, writer.Position);
        }
        public static (byte[], int) WorkerMoveToMovable(WorkerModel worker, MovableObjectNode targetMovable, bool resetCommand)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.WorkerMoveToMovable);
            writer.WriteLong(worker.instanceId);
            writer.WriteMovableNode(targetMovable);
            writer.WriteBool(resetCommand);
            return (result, writer.Position);
        }
        public static (byte[], int) WorkerMoveById(WorkerModel worker, string targetId)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.WorkerMoveById);
            writer.WriteLong(worker.instanceId);
            writer.WriteString(targetId);
            return (result, writer.Position);
        }
        public static (byte[], int) WorkerMoveByLink(WorkerModel worker, MapNode node, bool resetCommand)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.WorkerMoveByLink);
            writer.WriteLong(worker.instanceId);
            writer.WriteMapNode(node);
            writer.WriteBool(resetCommand);
            return (result, writer.Position);
        }
        public static (byte[], int) CancelWork(AgentModel agent)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.CancelWork);
            writer.WriteLong(agent.instanceId);
            return (result, writer.Position);
        }
        public static (byte[], int) CancelSuppress(AgentModel agent)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.CancelSuppress);
            writer.WriteLong(agent.instanceId);
            return (result, writer.Position);
        }
        public static (byte[], int) StartWorkCreature(AgentModel agent, CreatureModel creature, long WorkId)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.StartWorkCreature);
            writer.WriteLong(agent.instanceId);
            writer.WriteLong(creature.instanceId);
            writer.WriteLong(WorkId);
            return (result, writer.Position);
        }
        public static (byte[], int) StartWorkKit(AgentModel agent, CreatureModel creature)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.StartWorkKit);
            writer.WriteLong(agent.instanceId);
            writer.WriteLong(creature.instanceId);
            return (result, writer.Position);
        }
        public static (byte[], int) ProgressWorkResult(UseSkill skill, bool success)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.ProgressWorkResult);
            writer.WriteLong(skill.agent.instanceId);
            writer.WriteBool(success);
            return (result, writer.Position);
        }
        public static (byte[], int) Pause(PAUSECALL caller)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.Pause);
            writer.WriteByte((byte)caller);
            return (result, writer.Position);
        }
        public static (byte[], int) Resume(PAUSECALL caller)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.Resume);
            writer.WriteByte((byte)caller);
            return (result, writer.Position);
        }
        public static (byte[], int) Speed(int level)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.Speed);
            writer.WriteInt(level);
            return (result, writer.Position);
        }
        public static (byte[], int) SelectResearch(int id)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.SelectResearch);
            writer.WriteInt(id);
            return (result, writer.Position);
        }
        public static (byte[], int) ConfirmResearch(int id)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.ConfirmResearch);
            writer.WriteInt(id);
            return (result, writer.Position);
        }
        public static (byte[], int) WorkerDamageNoEffect(WorkerModel worker, UnitModel actor, float damage, bool revialHp, bool revialMental, RwbpType type, string specialDeadScene)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.WorkerDamageNoEffect);
            writer.WriteLong(worker.instanceId);
            writer.WriteLong(actor.instanceId);
            writer.WriteFloat(damage);
            writer.WriteBool(revialHp);
            writer.WriteBool(revialMental);
            writer.WriteByte((byte)type);
            writer.WriteString(specialDeadScene);
            return (result, writer.Position);
        }
        public static (byte[], int) WorkerDamage(WorkerModel worker, UnitModel actor, float damage, bool makeSpatteredBlood, bool revialHp, bool revialMental, RwbpType type, string specialDeadScene)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.WorkerDamage);
            writer.WriteLong(worker.instanceId);
            writer.WriteLong(actor.instanceId);
            writer.WriteFloat(damage);
            writer.WriteBool(makeSpatteredBlood);
            writer.WriteBool(revialHp);
            writer.WriteBool(revialMental);
            writer.WriteByte((byte)type);
            writer.WriteString(specialDeadScene);
            return (result, writer.Position);
        }
        public static (byte[], int) CreatureObserve(CreatureModel creature, string areaName, int oldLevel)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.CreatureObserve);
            writer.WriteLong(creature.instanceId);
            writer.WriteString(areaName);
            writer.WriteInt(oldLevel);
            return (result, writer.Position);
        }
        public static (byte[], int) CreateEquipment(int id, int cost, CreatureModel creature)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.CreateEquipment);
            writer.WriteInt(id);
            writer.WriteInt(cost);
            writer.WriteLong(creature.instanceId);
            return (result, writer.Position);
        }
        public static (byte[], int) GetEGO(AgentModel agent, int id)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.GetEGO);
            writer.WriteLong(agent.instanceId);
            writer.WriteInt(id);
            return (result, writer.Position);
        }
        public static (byte[], int) OverloadedCreatures(List<CreatureModel> creatures, float overloadTime, OverloadType type)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.Overload);
            writer.WriteInt(creatures.Count);
            foreach (CreatureModel creature in creatures)
            {
                writer.WriteLong(creature.instanceId);
            }
            writer.WriteFloat(overloadTime);
            writer.WriteByte((byte)type);
            return (result, writer.Position);
        }
        public static (byte[], int) CreatureMoveToNode(CreatureModel creature, MapNode node)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.CreatureMoveToNode);
            writer.WriteLong(creature.instanceId);
            writer.WriteMapNode(node);
            return (result, writer.Position);
        }
        public static (byte[], int) CreatureMoveToMovable(CreatureModel creature, MovableObjectNode node)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.CreatureMoveToMovable);
            writer.WriteLong(creature.instanceId);
            writer.WriteMovableNode(node);
            return (result, writer.Position);
        }
        public static (byte[], int) WaitingPassage(AgentModel agent, PassageObjectModel passage, SefiraEnum sefira)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.WaitingPassage);
            writer.WriteLong(agent.instanceId);
            writer.WriteByte((byte)sefira);
            writer.WriteString(passage.GetId());
            return (result, writer.Position);
        }
        public static (byte[], int) Suppress(AgentModel agent, UnitModel target, bool isAuto)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.Suppress);
            writer.WriteLong(agent.instanceId);
            writer.WriteLong(target.instanceId);
            writer.WriteBool(isAuto);
            return (result, writer.Position);
        }
        public static (byte[], int) SelectAno(long id)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.SelectAno);
            writer.WriteLong(id);
            return (result, writer.Position);
        }
        public static (byte[], int) InitSelectUI(long id1, long id2, long id3)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.InitSelectUI);
            writer.WriteLong(id1);
            writer.WriteLong(id2);
            writer.WriteLong(id3);
            return (result, writer.Position);
        }
        public static (byte[], int) SelectToExpand(SefiraEnum sefira)
        {
            byte[] result = MemoryPool.Rent(512);
            PacketWriter writer = new PacketWriter(result);
            writer.WriteByte((byte)PacketType.SelectToExpand);
            writer.WriteByte((byte)sefira);
            return (result, writer.Position);
        }
        /*
        public static Span<byte> Name(Span<byte> buffer)
        {
            PacketWriter writer = new(buffer);
            writer.WriteByte((byte)PacketType.);
            return buffer[..(writer.Position - 1)];
        }
        */
    }
    public class DeserializePacket
    {
        /*public static (AgentData, WorkerPrimaryStat, int, int, int) NewAgent(PacketReader reader)
        {
            bool customName = reader.ReadBool();
            AgentData data = new AgentData
            {
                isCustomName = customName
            };
            if (data.isCustomName)
            {
                data.CustomName = reader.ReadString();
                data.agentName = AgentNameList.instance.GetCustomNameByInfo(data.CustomName);
            }
            else data.agentName = AgentNameList.instance.GetName(reader.ReadInt());
            data.appearance.spriteSet.saveData.Eye.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.Eye.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.EyeClose.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.EyeClose.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.EyePanic.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.EyePanic.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.EyeDead.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.EyeDead.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.EyeBrow.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.EyeBrow.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.BattleEyeBrow.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.BattleEyeBrow.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.PanicEyeBrow.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.PanicEyeBrow.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.EyeColor.r = reader.ReadFloat();
            data.appearance.spriteSet.saveData.EyeColor.g = reader.ReadFloat();
            data.appearance.spriteSet.saveData.EyeColor.b = reader.ReadFloat();
            data.appearance.spriteSet.saveData.EyeColor.a = reader.ReadFloat();
            data.appearance.spriteSet.saveData.Mouth.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.Mouth.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.BattleMouth.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.BattleMouth.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.PanicMouth.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.PanicMouth.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.FrontHair.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.FrontHair.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.RearHair.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.RearHair.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.AttachmentHair.a = reader.ReadLong();
            data.appearance.spriteSet.saveData.AttachmentHair.b = reader.ReadInt();
            data.appearance.spriteSet.saveData.HairColor.r = reader.ReadFloat();
            data.appearance.spriteSet.saveData.HairColor.g = reader.ReadFloat();
            data.appearance.spriteSet.saveData.HairColor.b = reader.ReadFloat();
            data.appearance.spriteSet.saveData.HairColor.a = reader.ReadFloat();
            WorkerSpriteManager.instance.LoadWorkerSpriteSetBySaveData(data.appearance.spriteSet);
            int hp = reader.ReadInt();
            int mental = reader.ReadInt();
            int work = reader.ReadInt();
            int battle = reader.ReadInt();
            WorkerPrimaryStat stats = new WorkerPrimaryStat
            {
                hp = hp,
                mental = mental,
                work = work,
                battle = battle
            };
            int prefixId = reader.ReadInt();
            int suffixId = reader.ReadInt();
            int cost = reader.ReadInt();
            return (data, stats, prefixId, suffixId, cost);
        }
        public static (AgentModel, WorkerPrimaryStat, int, int, int) ChangeAgent(PacketReader reader)
        {
            long id = reader.ReadLong();
            AgentModel agent = AgentManager.instance.GetAgent(id) ?? AgentManager.instance.GetSpareAgent(id);
            int hp = reader.ReadInt();
            int mental = reader.ReadInt();
            int work = reader.ReadInt();
            int battle = reader.ReadInt();
            WorkerPrimaryStat stats = new WorkerPrimaryStat
            {
                hp = hp,
                mental = mental,
                work = work,
                battle = battle
            };
            int prefixId = reader.ReadInt();
            int suffixId = reader.ReadInt();
            int cost = reader.ReadInt();
            return (agent, stats, prefixId, suffixId, cost);
        }
        public static InitOfficers InitOfficers(PacketReader reader)
        {
            InitOfficers officers = new InitOfficers(SefiraManager.instance.GetSefira((SefiraEnum)reader.ReadByte()));
            int count = reader.ReadInt();
            for (int i = 0; i < count; i++)
            {
                long id = reader.ReadLong();
                string name = reader.ReadString();
                int maxHp = reader.ReadInt();
                int maxMental = reader.ReadInt();
                float baseMovement = reader.ReadFloat();
                WorkerSpriteSaveData data = new WorkerSpriteSaveData();
                data.Eye.a = reader.ReadLong();
                data.Eye.b = reader.ReadInt();
                data.EyeClose.a = reader.ReadLong();
                data.EyeClose.b = reader.ReadInt();
                data.EyePanic.a = reader.ReadLong();
                data.EyePanic.b = reader.ReadInt();
                data.EyeDead.a = reader.ReadLong();
                data.EyeDead.b = reader.ReadInt();
                data.EyeBrow.a = reader.ReadLong();
                data.EyeBrow.b = reader.ReadInt();
                data.BattleEyeBrow.a = reader.ReadLong();
                data.BattleEyeBrow.b = reader.ReadInt();
                data.PanicEyeBrow.a = reader.ReadLong();
                data.PanicEyeBrow.b = reader.ReadInt();
                data.EyeColor.r = reader.ReadFloat();
                data.EyeColor.g = reader.ReadFloat();
                data.EyeColor.b = reader.ReadFloat();
                data.EyeColor.a = reader.ReadFloat();
                data.Mouth.a = reader.ReadLong();
                data.Mouth.b = reader.ReadInt();
                data.BattleMouth.a = reader.ReadLong();
                data.BattleMouth.b = reader.ReadInt();
                data.PanicMouth.a = reader.ReadLong();
                data.PanicMouth.b = reader.ReadInt();
                data.FrontHair.a = reader.ReadLong();
                data.FrontHair.b = reader.ReadInt();
                data.RearHair.a = reader.ReadLong();
                data.RearHair.b = reader.ReadInt();
                data.AttachmentHair.a = reader.ReadLong();
                data.AttachmentHair.b = reader.ReadInt();
                data.HairColor.r = reader.ReadFloat();
                data.HairColor.g = reader.ReadFloat();
                data.HairColor.b = reader.ReadFloat();
                data.HairColor.a = reader.ReadFloat();
                string currNodeId = reader.ReadString();
                string nodeId = reader.ReadString();
                officers.officers.Add(new InitOfficers.OfficerData
                {
                    id = id,
                    name = name,
                    MaxHp = maxHp,
                    MaxMental = maxMental,
                    baseMovement = baseMovement,
                    setData = data,
                    currNodeId = currNodeId,
                    nodeId = nodeId
                });
            }
            return officers;
        }
        /*
        public static T Name(PacketReader reader)
        {

        }
        */
    }
}
