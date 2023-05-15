
using UnityEngine;

public class VoxelDebugMenu : MonoBehaviour
{
	[SerializeField] GameObject menuGameObject;

	void Start()
	{
		menuGameObject.SetActive(false);
	}

	public void OpenCloseMenu()
	{
		menuGameObject.SetActive(!menuGameObject.activeSelf);
	}

}
