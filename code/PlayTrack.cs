using System;
using System.IO;
using System.Net.Http;
using Sandbox.Library.RemoteAudio;

public class PlayTrack
{
	[ConCmd( "playtrack", Help = "URL of audio. Must be in quotes." )]
	internal static async void command( string audioUrl, float volume = 1 )
	{

		HttpResponseMessage response = null;
		try
		{
			var uri = new Uri( audioUrl );
			response = await Http.RequestAsync( uri.AbsoluteUri );
			if ( !response.Content.Headers.GetValues( "Content-Type" ).Any( type => type.Contains( "audio" ) ) )
				throw new FileNotFoundException( "Not an audio type" );						
		}
		catch ( Exception e )
		{
			Log.Warning( e );
			return;
		}


		await new RemoteAudio( volume ).LoadMusicFromWeb( response );


	}
}
