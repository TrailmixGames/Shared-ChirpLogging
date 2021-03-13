﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace WhiteSparrow.Shared.Logging
{
	public class LogChannelCompiler
	{
		
		private const string ListTemplate = @"
public class ListTemplate : WhiteSparrow.Shared.Logging.AbstractLogChannelList
{
	[UnityEngine.RuntimeInitializeOnLoadMethod]
	private static void RuntimeInitializeOnLoad()
	{
		WhiteSparrow.Shared.Logging.LogChannel.RegisterChannelTarget(s_ChannelList);
	}

	private static readonly System.Type[] s_ChannelList = new System.Type[]
	{
${TYPE_LIST}
	};
	
	public override System.Type[] GetChannelList()
	{
		return s_ChannelList;
	}
}
";


		[MenuItem("Tools/Chirp/Attempt the thing")]
		public static void Attempt()
		{
			var typesForGeneration = TypeCache.GetTypesWithAttribute<LogChannelAttribute>();
			// get all existing items
			var existingItems = FindExistingLogChannelsList(out var indexedChannels);

			if (typesForGeneration.Count == 0 && existingItems.Length == 0)
				return;

			if (existingItems.Length == 1)
			{
				if (TypeListCompare(typesForGeneration, indexedChannels))
				{
					Debug.Log("Type list match, generation skipped");
					return;
				}
			}

			foreach (var existingItem in existingItems)
			{
				AssetDatabase.DeleteAsset(existingItem.Item2);
			}

			if (typesForGeneration.Count > 0)
			{
				GenerateFile(typesForGeneration);
			}


		}


		private static Tuple<Type, string>[] FindExistingLogChannelsList(out HashSet<Type> indexedTypes)
		{
			indexedTypes = new HashSet<Type>();
			var existingListTypes = TypeCache.GetTypesDerivedFrom<AbstractLogChannelList>();
			if (existingListTypes.Count == 0)
				return Array.Empty<Tuple<Type, string>>();

			List<Tuple<Type, string>> output = new List<Tuple<Type, string>>();
			foreach (var type in existingListTypes)
			{
				var scriptableObject = ScriptableObject.CreateInstance(type);
				scriptableObject.hideFlags = HideFlags.HideAndDontSave;
				var monoScript = MonoScript.FromScriptableObject(scriptableObject);
				string path = AssetDatabase.GetAssetPath(monoScript);
				if (string.IsNullOrEmpty(path))
				{
					Debug.LogError($"Chirp: LogChannel List generation found type {type.FullName} for replacement when generating, but couldn't find the file path. It's possible that the file name is not the same as the Type name.");
					continue;
				}

				if (scriptableObject is AbstractLogChannelList logChannelList)
				{
					var indexedChannels = logChannelList.GetChannelList();
					foreach (var channel in indexedChannels)
					{
						indexedTypes.Add(channel);
					}
				}
				
				ScriptableObject.DestroyImmediate(scriptableObject, true);
				
				output.Add(Tuple.Create<Type, string>(type, path));
			}

			return output.ToArray();
		}
		
		
		private static void GenerateFile(TypeCache.TypeCollection typeList)
		{
			StringBuilder typeListBuilder = new StringBuilder();
			foreach (var type in typeList)
			{
				typeListBuilder.AppendLine($"		typeof({type.FullName}),");
			}
			
			string content = ListTemplate.Replace("${TYPE_LIST}", typeListBuilder.ToString());
			FileInfo targetFile = new FileInfo(Path.Combine(Application.dataPath,"Scripts/ListTemplate.cs"));
			File.WriteAllText(targetFile.FullName, content);
			
			AssetDatabase.Refresh();
		}

		private static bool TypeListCompare(TypeCache.TypeCollection typeCandidates, HashSet<Type> typesRegistered)
		{
			if (typeCandidates.Count != typesRegistered.Count)
				return false;

			foreach (var type in typeCandidates)
			{
				if (!typesRegistered.Contains(type))
					return false;
			}
			
			return true;
		}
		
	}
}