using BepInEx;
using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using DiskCardGame;
using HarmonyLib;


namespace TotemStartMod
{

	[BepInPlugin( PluginGuid, PluginName, PluginVersion )]
	public class Plugin : BaseUnityPlugin
	{
		private const string PluginGuid = "MADH.inscryption.TotemStartMod";
		private const string PluginName = "TotemStartMod";
		private const string PluginVersion = "1.1.0";

		internal static ManualLogSource Log;

		public bool getRandom()
		{
			return Config.Bind( PluginName, "Random Totem", true, new BepInEx.Configuration.ConfigDescription( "Load a random totem at the start of the run" ) ).Value;
		}

		public (string head, string body) getTotem()
		{
			return (Config.Bind( PluginName, "Totem Head", "Squirrel" ).Value, Config.Bind( PluginName, "Totem Body", "DrawCopy" ).Value);
		}

		private void Awake()
		{
			Logger.LogInfo( $"Loaded {PluginName}!" );
			Plugin.Log = base.Logger;

			Harmony harmony = new Harmony(PluginGuid);
			harmony.PatchAll();

			bool random = getRandom();

			var totem = getTotem();

			if ( random )
			{
				if ( totem.head == null || totem.head == "" )
					Logger.LogError( "Cannot load totem with no head" );
				else if ( !Tribes.ContainsKey( totem.head ) )
					Logger.LogError( $"Cannot load totem with head name \"{totem.head}\"" );

				if ( totem.body == null || totem.body == "" )
					Logger.LogError( "Cannot load totem with no body" );
				else if ( !Abilities.ContainsKey( totem.body ) )
					Logger.LogError( $"Cannot load totem with body name \"{totem.body}\"" );
			}

		}

		[HarmonyPatch( typeof( RunState ), "InitializeStarterDeckAndItems" )]
		public class CardInteractionTestRunState : RunState
		{
			public static bool Prefix( ref RunState __instance )
			{
				Plugin p = new Plugin();

				var strTotem = p.getTotem();

				( Tribe head, Ability body ) totem = ( Tribes[strTotem.head], Abilities[strTotem.body] );

				if ( p.getRandom() )
				{
					Tribe head = (Tribe)SeededRandom.Range( 1, Tribes.Count, SaveManager.SaveFile.GetCurrentRandomSeed() );
					Ability body = (Ability)SeededRandom.Range( 1, Abilities.Count, SaveManager.SaveFile.GetCurrentRandomSeed() );

					totem = ( head, body );
				}

				__instance.totemTops.Add( totem.head );
				__instance.totemBottoms.Add( totem.body );

				TotemDefinition totemDefinition = new TotemDefinition();
				totemDefinition.tribe = totem.head;
				totemDefinition.ability = totem.body;
				Run.totems.Clear();
				Run.totems.Add( totemDefinition );

				return true;
			}
		}

		public static Dictionary<string, Tribe> Tribes
			=   Enum.GetValues( typeof( Tribe ) )
					.Cast< Tribe >()
					.ToDictionary( t => t.ToString(), t => t );

		public static Dictionary<string, Ability> Abilities
			=   Enum.GetValues( typeof( Ability ) )
					.Cast< Ability >()
					.ToDictionary( t => t.ToString(), t => t );
	};
}