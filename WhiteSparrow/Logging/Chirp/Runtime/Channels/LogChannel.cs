﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace WhiteSparrow.Shared.Logging
{
	public class LogChannel : IEquatable<LogChannel>, IEquatable<string>
	{
		public Color color;
		public readonly string id;
		public readonly string name;

		public bool isFallback = false;

		public LogChannel(string name) : this(name, CreateColorHash(name))
		{
		}

		public LogChannel(string name, Color color)
		{
			id = name.ToLower();
			this.name = name;
			this.color = color;

			RegisterChannel(this);
		}

		private static Color CreateColorHash(string id)
		{
			var characters = id.ToCharArray();
			double hash = 0;
			for (var i = 0; i < characters.Length; i++)
				hash = char.GetNumericValue(characters[i]) + (((int) hash << 5) - hash);

			var h = (float) hash % 200;
			var v = (float) hash % 240;

			return Color.HSVToRGB(Mathf.Abs(h) / 200f,
				0.9f,
				Mathf.Clamp(Mathf.Abs(v) / 240f, 0.7f, 1f));
		}

		// custom operator to use string id for easy assignment
		public static implicit operator LogChannel(string logChannel)
		{
			return Get(logChannel);
		}
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
#else
		[Conditional("CHIRP")]
#endif
		public void Debug(params object[] message)
		{
			Chirp.AddLog(this, LogLevel.Debug, message);
		}

		
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
		[Conditional("LogLevel1"), Conditional("LogLevelDefault")]
#else
		[Conditional("CHIRP")]
#endif
		public void Log(params object[] message)
		{
			Chirp.AddLog(this, LogLevel.Log, message);
		}
		
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
		[Conditional("LogLevel1"), Conditional("LogLevelDefault")]
		[Conditional("LogLevel2"), Conditional("LogLevelInfo")]
#else
		[Conditional("CHIRP")]
#endif
		public void Info(params object[] message)
		{
			Chirp.AddLog(this, LogLevel.Info, message);
		}

		
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
		[Conditional("LogLevel1"), Conditional("LogLevelDefault")]
		[Conditional("LogLevel2"), Conditional("LogLevelInfo")]
		[Conditional("LogLevel3"), Conditional("LogLevelWarning")]
#else
		[Conditional("CHIRP")]
#endif
		public void Warning(params object[] message)
		{
			Chirp.AddLog(this, LogLevel.Warning, message);
		}
		
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
		[Conditional("LogLevel1"), Conditional("LogLevelDefault")]
		[Conditional("LogLevel2"), Conditional("LogLevelInfo")]
		[Conditional("LogLevel3"), Conditional("LogLevelWarning")]
		[Conditional("LogLevel4"), Conditional("LogLevelAssert")]
#else
		[Conditional("CHIRP")]
#endif
		public void Assert(LogChannel channel, bool condition, params object[] message)
		{
			if (condition)
				return;
			Chirp.AddLog(this, LogLevel.Assert, message);
		}
		
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
		[Conditional("LogLevel1"), Conditional("LogLevelDefault")]
		[Conditional("LogLevel2"), Conditional("LogLevelInfo")]
		[Conditional("LogLevel3"), Conditional("LogLevelWarning")]
		[Conditional("LogLevel4"), Conditional("LogLevelAssert")]
		[Conditional("LogLevel5"), Conditional("LogLevelError")]
#else
		[Conditional("CHIRP")]
#endif
		public void Error(params object[] message)
		{
			Chirp.AddLog(this, LogLevel.Error, message);
		}
		
		
#if CHIRP
		[Conditional("LogLevel0"), Conditional("LogLevelDebug")]
		[Conditional("LogLevel1"), Conditional("LogLevelDefault")]
		[Conditional("LogLevel2"), Conditional("LogLevelInfo")]
		[Conditional("LogLevel3"), Conditional("LogLevelWarning")]
		[Conditional("LogLevel4"), Conditional("LogLevelAssert")]
		[Conditional("LogLevel5"), Conditional("LogLevelError")]
		[Conditional("LogLevel6"), Conditional("LogLevelException")]
		[Conditional("CHIRP")]
#else
		[Conditional("CHIRP")]
#endif
		public void Exception(Exception exception, params object[] message)
		{
			Chirp.AddException(this, LogLevel.Exception, exception, message);
		}
		
#region Comparison

		public bool Equals(LogChannel other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return id == other.id;
		}

		public bool Equals(string other)
		{
			return other != null && id == other.ToLower();
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj is string channelId)
				return Equals(channelId);
			if (obj is LogChannel channel)
				return Equals(channel);
			return false;
		}

		public override int GetHashCode()
		{
			return id != null ? id.GetHashCode() : 0;
		}

		public static bool operator ==(LogChannel lhs, LogChannel rhs)
		{
			if (ReferenceEquals(null, lhs))
				return ReferenceEquals(null, rhs);
			return lhs.Equals(rhs);
		}

		public static bool operator !=(LogChannel lhs, LogChannel rhs)
		{
			return !(lhs == rhs);
		}
		
#endregion

#region Static Registry

		private static readonly Dictionary<string, LogChannel> s_IdToInstanceMapping =
			new Dictionary<string, LogChannel>();

		private static readonly List<string> s_IdList = new List<string>();
		private static string[] s_IdListCache;

		private void RegisterChannel(LogChannel channel)
		{
			if (s_IdToInstanceMapping.ContainsKey(channel.id))
				return;

			s_IdToInstanceMapping.Add(channel.id, channel);
			s_IdList.Add(channel.id);
		}

		public static LogChannel Get(string id)
		{
			if (s_IdToInstanceMapping.TryGetValue(id, out var channel))
				return channel;

			return new LogChannel(id);
		}

		public static string[] GetAllChannelIds()
		{
			if (s_IdListCache == null)
				s_IdListCache = s_IdList.ToArray();
			return s_IdListCache;
		}

		public static bool HasChannel(string id)
		{
			return s_IdList.Contains(id);
		}

#endregion

#region Type Registry

		private static readonly Dictionary<Type, LogChannel> s_TypeToInstanceMapping =
			new Dictionary<Type, LogChannel>();

		public static LogChannel RegisterChannelTarget(Type type)
		{
			if (s_TypeToInstanceMapping.TryGetValue(type, out var existingChannel))
				return existingChannel;

			var channelName = type.Name;
			var channel = Get(channelName);
			s_TypeToInstanceMapping.Add(type, channel);
			return channel;
		}

		public static void RegisterChannelTarget(IEnumerable<Type> types)
		{
			foreach (var type in types) RegisterChannelTarget(type);
		}

		public static LogChannel GetForTarget(Type type)
		{
			if (s_TypeToInstanceMapping.TryGetValue(type, out var existingChannel))
				return existingChannel;
			return null;
		}

#endregion
	}
}