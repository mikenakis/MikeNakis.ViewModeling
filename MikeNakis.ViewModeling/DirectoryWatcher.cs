namespace MikeNakis.ViewModeling;

using System.Collections.Generic;
using MikeNakis.Kit;
using MikeNakis.Kit.Extensions;
using MikeNakis.Kit.FileSystem;
using static MikeNakis.Kit.GlobalStatics;
using Sys = System;
using SysIo = System.IO;

public sealed class DirectoryWatcher : Sys.IDisposable
{
	readonly LifeGuard lifeGuard = LifeGuard.Create();
	readonly Platform platform;
	readonly string name;
	public DirectoryPath Directory { get; }
	readonly string extension;
	readonly Sys.TimeSpan delay;
	readonly SysIo.FileSystemWatcher fileSystemWatcher;
	public event Sys.Action<string>? EventHandler;
	readonly HashSet<string> events = new();

	public DirectoryWatcher( Platform platform, string name, DirectoryPath directory, string extension, bool includeSubdirectories, Sys.TimeSpan delay )
	{
		this.platform = platform;
		this.name = name;
		Directory = directory;
		this.extension = extension;
		this.delay = delay;
		fileSystemWatcher = new SysIo.FileSystemWatcher();
		fileSystemWatcher.Path = directory.Path;
		fileSystemWatcher.IncludeSubdirectories = includeSubdirectories;
		//fileSystemWatcher.Filter                = "*";
		fileSystemWatcher.NotifyFilter = SysIo.NotifyFilters.FileName |
				SysIo.NotifyFilters.DirectoryName | //
				SysIo.NotifyFilters.Attributes |
				SysIo.NotifyFilters.Size |
				SysIo.NotifyFilters.LastWrite |
				SysIo.NotifyFilters.LastAccess | //
				SysIo.NotifyFilters.CreationTime |
				SysIo.NotifyFilters.Security;
		fileSystemWatcher.Changed += onFileSystemWatcherNormalEvent;
		fileSystemWatcher.Created += onFileSystemWatcherNormalEvent;
		fileSystemWatcher.Deleted += onFileSystemWatcherNormalEvent;
		fileSystemWatcher.Error += onFileSystemWatcherErrorEvent;
		fileSystemWatcher.Renamed += onFileSystemWatcherNormalEvent;
		fileSystemWatcher.EnableRaisingEvents = true;
	}

	public void /*IDisposable.*/ Dispose()
	{
		Assert( lifeGuard.IsAliveAssertion() );
		fileSystemWatcher.Dispose();
		lifeGuard.Dispose();
	}

	void onFileSystemWatcherNormalEvent( object sender, SysIo.FileSystemEventArgs e )
	{
		Assert( sender == fileSystemWatcher );
		FilePath fullPath = FilePath.FromAbsolutePath( e.FullPath );
		if( !fullPath.EndsWith( extension ) )
			return;
		string relativePath = Directory.GetRelativePath( fullPath );
		if( Identity( false ) )
			Log.Info( $"{typeof( DirectoryWatcher ).Name} {name}: {e.ChangeType} '{relativePath}'" );
		lock( events )
		{
			if( events.Contains( relativePath ) )
			{
				Log.Debug( $"{typeof( DirectoryWatcher ).Name} {name}: Event is already in queue, ignoring: '{relativePath}'." );
				return;
			}
			events.Add( relativePath );
			if( events.Count == 1 )
				platform.EventDriver.Proxy.Post( "Filesystem event", OperationType.Minor, onImmediateUiThreadEvent );
		}
	}

	void onFileSystemWatcherErrorEvent( object sender, SysIo.ErrorEventArgs e )
	{
		Assert( sender == fileSystemWatcher );
		Log.Warn( $"{typeof( DirectoryWatcher ).Name} {name}", e.GetException() );
	}

	void onImmediateUiThreadEvent()
	{
		//The file is probably still being written, so delay handling it.
		platform.TimerManager.After( "DirectoryWatcherEvent", OperationType.Major, delay, onDeferredUiThreadEvent );
	}

	void onDeferredUiThreadEvent()
	{
		foreach( string @event in getAndClearEvents() )
			EventHandler?.Invoke( @event );
	}

	IEnumerable<string> getAndClearEvents()
	{
		lock( events )
		{
			IEnumerable<string> events = this.events.Collect();
			this.events.Clear();
			return events;
		}
	}
}
