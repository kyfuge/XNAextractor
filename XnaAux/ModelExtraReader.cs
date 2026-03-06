using System.Collections.Generic;
using Microsoft.Xna.Framework.Content;

namespace AnimationAux;

public class ModelExtraReader : ContentTypeReader<ModelExtra>
{
	protected override ModelExtra Read(ContentReader input, ModelExtra existingInstance)
	{
		ModelExtra modelExtra = new ModelExtra();
		modelExtra.Skeleton = input.ReadObject<List<int>>();
		modelExtra.Clips = input.ReadObject<List<AnimationClip>>();
		return modelExtra;
	}
}
