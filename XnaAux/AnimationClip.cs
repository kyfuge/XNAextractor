using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace AnimationAux;

public class AnimationClip
{
	public class Keyframe
	{
		public double Time;

		public Quaternion Rotation;

		public Vector3 Translation;

		public Matrix Transform
		{
			get
			{
				return Matrix.CreateFromQuaternion(Rotation) * Matrix.CreateTranslation(Translation);
			}
			set
			{
				Matrix matrix = value;
				matrix.Right = Vector3.Normalize(matrix.Right);
				matrix.Up = Vector3.Normalize(matrix.Up);
				matrix.Backward = Vector3.Normalize(matrix.Backward);
				Rotation = Quaternion.CreateFromRotationMatrix(matrix);
				Translation = matrix.Translation;
			}
		}
	}

	public class Bone
	{
		private string name = "";

		private List<Keyframe> keyframes = new List<Keyframe>();

		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
			}
		}

		public List<Keyframe> Keyframes => keyframes;
	}

	private List<Bone> bones = new List<Bone>();

	public string Name;

	public double Duration;

	public List<Bone> Bones => bones;
}
