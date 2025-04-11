using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

public class CustomPlayerColliderController : UdonSharpBehaviour
{
	public float moveSpeed = 1;

	public float rotationStrength = 1;

	public float jumpStrength = 1;

	public float positionOffset;

	public float rotationOffset;

	public Vector2 input2DMove;

	public Vector3 input3DRotate;

	public bool inputJump;

	public float avatarHeight;

	public bool onGround = false;

	public bool inStation = false;

	public CapsuleCollider customCollider;

	public Rigidbody body;

	public VRCStation station;

	public VRCPlayerApi player;

	public void Start()
	{
		customCollider = GetComponent<CapsuleCollider>();

		body = GetComponent<Rigidbody>();

		station = GetComponent<VRCStation>();

		body.velocity = Vector3.zero;

		player = null;

		onGround = false;

		inputJump = false;

		inStation = false;
	}

	public override void OnStationEntered(VRCPlayerApi detectedPlayer)
	{
		if (detectedPlayer == player)
		{
			inStation = true;

			Debug.Log("Station entered!");
		}
	}

	public override void OnStationExited(VRCPlayerApi detectedPlayer)
	{
		if (detectedPlayer == player)
		{
			inStation = false;
		}
	}

	public void Update()
	{
		if (input2DMove.magnitude < 0.1f)
		{
			if (player == null)
			{
				player = Networking.LocalPlayer;

				if (player == null) return;
			}
			var headPosition = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position;

			headPosition = headPosition - Vector3.up * Vector3.Dot(Vector3.up, headPosition);

			var customColliderPos = transform.position;

			var customColliPosXZ = customColliderPos - Vector3.up * Vector3.Dot(Vector3.up, customColliderPos);

			var currentStationPositionOffset = (customColliPosXZ - headPosition).magnitude;

			var headQuaternion = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

			float currentStationRotationOffset;

			var headForward = headQuaternion * Vector3.forward;

			var headForwardUpDownValue = Vector3.Dot(headForward, Vector3.up);

			if (headForwardUpDownValue > -0.25f && headForwardUpDownValue < 0.75f)
			{
				headForward = Vector3.ProjectOnPlane(headForward, Vector3.up).normalized;

				currentStationRotationOffset = Vector3.SignedAngle(headForward, transform.forward, Vector3.up);
			}
			else
			{
				var headRight = headQuaternion * Vector3.right;

				headRight = Vector3.ProjectOnPlane(headRight, Vector3.up).normalized;

				currentStationRotationOffset = Vector3.SignedAngle(headRight, transform.right, Vector3.up);
			}

			if (currentStationRotationOffset < 0)
			{
				currentStationRotationOffset *= -1;
			}

			if (currentStationPositionOffset > positionOffset)
			{
				Debug.Log("Position Offset: " + currentStationPositionOffset);
			}

			if (currentStationRotationOffset > rotationOffset)
			{
				Debug.Log("Rotation Offset: " + currentStationRotationOffset);
			}

			if (!inStation)
			{
				Debug.Log("Station Not set!");
			}

			if (!inStation || currentStationPositionOffset > positionOffset || currentStationRotationOffset > rotationOffset)
			{
				if (inStation)
				{
					station.ExitStation(player);
				}
				else if (currentStationPositionOffset > positionOffset || currentStationRotationOffset > rotationOffset)
				{
					transform.position = player.GetPosition();
					transform.rotation = player.GetRotation();
				}
				else
				{
					Networking.LocalPlayer.UseAttachedStation();
				}
			}

			var newAvatarHeight = (player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).position - player.GetPosition()).y * 1.05f;

			if (Mathf.Abs(newAvatarHeight - avatarHeight) > 0.01f)
			{
				avatarHeight = newAvatarHeight;

				customCollider.height = avatarHeight;

				customCollider.radius = avatarHeight * 0.125f;

				var colliderCenter = customCollider.center;

				colliderCenter.y = avatarHeight * 0.5f;

				customCollider.center = colliderCenter;
			}
		}
	}

	public override void InputMoveHorizontal(float input, UdonInputEventArgs args)
	{
		input2DMove.x = input;
	}

	public override void InputMoveVertical(float input, UdonInputEventArgs args)
	{
		input2DMove.y = input;
	}

	public override void InputLookHorizontal(float input, UdonInputEventArgs args)
	{
		input3DRotate.y = input;
	}

	public void InputJump()
	{
		if (onGround)
		{
			inputJump = true;
		}
	}

	public void OnCollisionStay(Collision collision)
	{
		foreach (var contact in collision.contacts)
		{
			if (Vector3.Dot(contact.normal, Vector3.up) > 0.76f)
			{
				onGround = true;
				break;
			}
		}
	}

	public void OnCollisionExit(Collision collision)
	{
		onGround = false;
	}

	public void FixedUpdate()
	{
		input2DMove = input2DMove.normalized;

		if (input2DMove.magnitude > 0)
		{
			var headQuaternion = player.GetTrackingData(VRCPlayerApi.TrackingDataType.Head).rotation;

			var headForward = headQuaternion * Vector3.forward;

			headForward = (headForward - Vector3.up * Vector3.Dot(headForward, Vector3.up)).normalized;

			var headRight = headQuaternion * Vector3.right;

			headRight = (headRight - Vector3.up * Vector3.Dot(headRight, Vector3.up)).normalized;

			var velocityF = headForward * input2DMove.y;

			var velocityR = headRight * input2DMove.x;

			var velocityFinal = (velocityF + velocityR) * moveSpeed * avatarHeight;

			var currentVelocity = body.velocity;

			var upVelocity = Vector3.up * Vector3.Dot(currentVelocity, Vector3.up);

			body.velocity = velocityFinal + upVelocity;
		}
		else
		{
			var currentVelocity = body.velocity;

			var upVelocity = Vector3.up * Vector3.Dot(currentVelocity, Vector3.up);

			body.velocity = upVelocity;
		}

		if (input3DRotate.magnitude > 0)
		{
			transform.Rotate(input3DRotate * rotationStrength);
		}
		
		if (inputJump && onGround)
		{
			var jumpForce = Vector3.up * jumpStrength + Vector3.up * avatarHeight;

			var newVelocity = body.velocity + jumpForce;

			body.velocity = newVelocity;

			inputJump = false;
			onGround = false;
		}
	}
}