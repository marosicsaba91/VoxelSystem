using UnityEngine;
public class Avatar : MonoBehaviour
{
	// [Header("Body References")]
	// [SerializeField] Transform body = null;


	[Header("Settings")]
	[SerializeField] float moveSpeed = 1;

	[Header("Physics")]
	[SerializeField] float gravity = 10;
	[SerializeField] float jumpSpeed = 50;
	[SerializeField] float linearDrag = 0.1f;

	[Header("Raycasts")]
	[SerializeField] float raycastOriginHeight = 0.25f;
	[SerializeField] float raycastR = 1;
	[SerializeField] float raycastLength = 0;
	[SerializeField, Range(3, 20)] int raycastCount = 5;
	[SerializeField] float legHeight = 0.65f;
	[SerializeField] float epsilon = 0.1f;

	float _verticalVelocity = 0;


	// Update is called once per frame
	void Update()
	{
		// Vector3 move = PlayerInput.GetMovementVector(); 
		// move = cam.Facing.Rotate(move);
		// transform.localPosition += body.TransformVector(move) * (moveSpeed * Time.deltaTime);


		if (PlayerInput.Jump)
			_verticalVelocity = jumpSpeed;

	}

	void FixedUpdate()
	{
		_verticalVelocity -= gravity * Time.fixedDeltaTime;
		_verticalVelocity += (_verticalVelocity > 0 ? -1 : 1) * (_verticalVelocity * _verticalVelocity) * linearDrag * Time.fixedDeltaTime;

		// OPTIM NEEDED
		RayCast();

		// Falling
		if (_verticalVelocity != 0)
		{
			transform.Translate(new Vector3(0, _verticalVelocity, 0), Space.World);
		}
		if (_targetPositionY != null)
		{
			float dist = _targetPositionY.Value - transform.position.y;
			transform.Translate(new Vector3(0, Mathf.Min(dist, moveSpeed * Time.fixedDeltaTime), 0), Space.World);
		}
	}

	float? _targetPositionY = null;

	void RayCast()
	{
		_targetPositionY = null;

		float d = legHeight + (epsilon / 2f) + raycastOriginHeight;

		foreach (Ray r in GetRaysToCast())
		{
			if (!Physics.Raycast(r, out RaycastHit hitInfo, raycastLength))
				continue;
			if (!(hitInfo.distance < d))
				continue;
			_verticalVelocity = 0;
			_targetPositionY = r.origin.y - hitInfo.distance + legHeight;
			break;
		}
	}

	Ray[] GetRaysToCast()
	{
		Ray[] result = new Ray[raycastCount];

		Vector3 center = transform.position;
		center.y += raycastOriginHeight;
		Vector3 right = Vector3.right * raycastR;
		Vector3 forward = Vector3.forward * raycastR;

		float roundSlice = 2f * Mathf.PI / raycastCount;

		for (int i = 0; i < result.Length; i++)
		{
			float angle = roundSlice * i;
			Vector3 origin = center + (right * Mathf.Sin(angle)) + (forward * Mathf.Cos(angle));
			result[i] = new Ray(origin, Vector3.down);
		}

		return result;
	}
}
