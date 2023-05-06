
using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoxelSystem
{
	class PlayModeHandleManager : MonoBehaviour
	{
		List<PlayModeHandele> unUsedPool = new List<PlayModeHandele>();
		List<PlayModeHandele> pool = new List<PlayModeHandele>();

		internal void PutBack(PlayModeHandele playModeHandele) => throw new NotImplementedException();
	}
}
