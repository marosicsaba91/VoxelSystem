
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	class PlayModeHandleManager : MonoBehaviour
	{
		readonly List<PlayModeHandele> unUsedPool = new();
		readonly List<PlayModeHandele> pool = new();

		internal void PutBack(PlayModeHandele playModeHandele) => throw new NotImplementedException();
	}
}
