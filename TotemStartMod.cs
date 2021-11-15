using System;
using System.Collections.Generic;
using System.Linq;

using DiskCardGame;

using BepInEx;
using BepInEx.Logging;

using HarmonyLib;


namespace TotemStartMod
{

	[BepInPlugin( PluginGuid, PluginName, PluginVersion )]
	public class Plugin : BaseUnityPlugin
	{
		private const string PluginGuid = "MADH.inscryption.TotemStartMod";
		private const string PluginName = "TotemStartMod";
		private const string PluginVersion = "1.1.1";

		internal static ManualLogSource Log;

		public bool GetRandom() => Config.Bind( PluginName, "Random Totem", true, new BepInEx.Configuration.ConfigDescription( "Load a random totem at the start of the run" ) ).Value;

		public (string head, string body) GetTotem() => (Config.Bind( PluginName, "Totem Head", "Squirrel" ).Value, Config.Bind( PluginName, "Totem Body", "DrawCopy" ).Value);

		private void Awake()
		{
			Logger.LogInfo( $"Loaded {PluginName}!" );
			Plugin.Log = base.Logger;

			Harmony harmony = new Harmony(PluginGuid);
			harmony.PatchAll();

			Tribes.Remove( Tribe.None.ToString() );
			Tribes.Remove( Tribe.NUM_TRIBES.ToString() );
			Abilities.Remove( Ability.None.ToString() );
			Abilities.Remove( Ability.NUM_ABILITIES.ToString() );

			bool random = GetRandom();

			var (head, body) = GetTotem();

			if ( !random )
			{
				if ( head == null || head == "" )
					Logger.LogError( "Cannot load totem with no head" );
				else if ( !Tribes.ContainsKey( head ) )
					Logger.LogError( $"Cannot load totem with head name \"{ head }\"" );

				if ( body == null || body == "" )
					Logger.LogError( "Cannot load totem with no body" );
				else if ( !Abilities.ContainsKey( body ) )
					Logger.LogError( $"Cannot load totem with body name \"{ body }\"" );
			}

		}

		[HarmonyPatch( typeof( RunState ), "InitializeStarterDeckAndItems" )]
		public class CardInteractionTestRunState : RunState
		{
			public static bool Prefix( ref RunState __instance )
			{
				Plugin p = new Plugin();

				bool random = p.GetRandom();

				var (strhead, strbody) = p.GetTotem();

				( Tribe head, Ability body ) totem = ( Tribe.None, Ability.None );

				if ( random )
				{
					Tribe head = (Tribe)SeededRandom.Range( 1, Tribes.Count, SaveManager.SaveFile.GetCurrentRandomSeed() );
					Ability body = (Ability)SeededRandom.Range( 1, Abilities.Count, SaveManager.SaveFile.GetCurrentRandomSeed() );

					totem = (head, body);
				}
				else if ( Tribes.ContainsKey( strhead ) )
				{
					if ( Abilities.ContainsKey( strbody ) )
						totem = (Tribes[ strhead ], Abilities[ strbody ]);
				}

				if ( totem.head == Tribe.None || totem.body == Ability.None )
				{
					Log.LogError( "Could not load invalid totem" );
					return false;
				}

				__instance.totemTops.Add( totem.head );
				__instance.totemBottoms.Add( totem.body );

				TotemDefinition totemDefinition = new TotemDefinition
				{
					tribe = totem.head,
					ability = totem.body
				};
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