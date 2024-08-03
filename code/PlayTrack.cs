using Sandbox.Library.RemoteAudio;
using Sandbox.Utility;

public class PlayTrack
{
	[ConCmd( "playtrack", Help = "URL of audio. Must be in quotes." )]
	internal static async void command( string audioUrl )
	{
		var audio = new RemoteAudio();

		GameObject localMesh = null;

		audio.OnEnabled = (SoundHandle handle) =>
		{
			CameraComponent Camera = Game.ActiveScene.GetAllComponents<CameraComponent>().FirstOrDefault();
			handle.Position = Camera.Transform.Position;
			handle.Occlusion = false;


			var config = new CloneConfig
			{
				Name = $"Radinho - {Steam.PersonaName}",
				Transform = Camera.Transform.World,
				PrefabVariables = new Dictionary<string, object>
						{
							{ "Owner", Steam.PersonaName },
						}
			};

			localMesh = GameObject.Clone( "prefabs/radinho.prefab", config );
			localMesh.Tags.Add( "noblockaudio" );
			localMesh.NetworkSpawn();
		};

		
		audio.OnUpdate = ( SoundHandle handle ) =>
		{
			if ( localMesh != null )
				handle.Position = localMesh.Transform.Position;
		};
		

		audio.OnDestroy = () =>
		{
			localMesh?.Destroy();
		};


		await audio.Play( audioUrl );
	}
}
