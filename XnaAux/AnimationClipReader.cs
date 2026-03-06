using Microsoft.Xna.Framework.Content;

namespace AnimationAux;

public class AnimationClipReader : ContentTypeReader<AnimationClip>
{
	protected override AnimationClip Read(ContentReader input, AnimationClip existingInstance)
	{
		AnimationClip animationClip = new AnimationClip();
		animationClip.Name = input.ReadString();
		animationClip.Duration = input.ReadDouble();
		int num = input.ReadInt32();
		for (int i = 0; i < num; i++)
		{
			AnimationClip.Bone bone = new AnimationClip.Bone();
			animationClip.Bones.Add(bone);
			bone.Name = input.ReadString();
			int num2 = input.ReadInt32();
			for (int j = 0; j < num2; j++)
			{
				AnimationClip.Keyframe keyframe = new AnimationClip.Keyframe();
				keyframe.Time = input.ReadDouble();
				keyframe.Rotation = input.ReadQuaternion();
				keyframe.Translation = input.ReadVector3();
				bone.Keyframes.Add(keyframe);
			}
		}
		return animationClip;
	}
}
