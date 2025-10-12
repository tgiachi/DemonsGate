using SquidCraft.Services.Game.Generation.Noise;

namespace SquidCraft.Tests.Game.Generation.Noise;

[TestFixture]
public class FastNoiseLiteTests
{
    [Test]
    public void Generate_ShouldReturnSameValueForSameSeedAndPoint()
    {
        var noiseA = new FastNoiseLite(1337);
        noiseA.SetNoiseType(NoiseType.Perlin);
        noiseA.SetFrequency(0.01f);

        var noiseB = new FastNoiseLite(1337);
        noiseB.SetNoiseType(NoiseType.Perlin);
        noiseB.SetFrequency(0.01f);

        const float x = 12.3f;
        const float y = 45.6f;
        const float z = 78.9f;
        var valueA = noiseA.GetNoise(x, y, z);
        var valueB = noiseB.GetNoise(x, y, z);

        Assert.That(valueB, Is.EqualTo(valueA));
    }

    [Test]
    public void Generate_ShouldDifferWithDifferentSeeds()
    {
        var noiseA = new FastNoiseLite(42);
        noiseA.SetNoiseType(NoiseType.Perlin);

        var noiseB = new FastNoiseLite(99);
        noiseB.SetNoiseType(NoiseType.Perlin);

        const float x = 10f;
        const float y = 20f;
        const float z = 30f;
        var valueA = noiseA.GetNoise(x, y, z);
        var valueB = noiseB.GetNoise(x, y, z);

        Assert.That(valueB, Is.Not.EqualTo(valueA));
    }

    [Test]
    public void FractalSettings_ShouldRespectOctaveCount()
    {
        var singleOctave = new FastNoiseLite();
        singleOctave.SetNoiseType(NoiseType.OpenSimplex2);
        singleOctave.SetFractalType(FractalType.FBm);
        singleOctave.SetFractalOctaves(1);
        singleOctave.SetFrequency(0.02f);

        var multiOctave = new FastNoiseLite();
        multiOctave.SetNoiseType(NoiseType.OpenSimplex2);
        multiOctave.SetFractalType(FractalType.FBm);
        multiOctave.SetFractalOctaves(4);
        multiOctave.SetFrequency(0.02f);

        const float x = 5f;
        const float y = 6f;
        const float z = 7f;
        var singleValue = singleOctave.GetNoise(x, y, z);
        var multiValue = multiOctave.GetNoise(x, y, z);

        Assert.That(singleValue, Is.Not.EqualTo(multiValue));
    }

    [Test]
    public void DomainWarp_ShouldModifyCoordinates()
    {
        var noise = new FastNoiseLite();
        noise.SetDomainWarpType(DomainWarpType.OpenSimplex2);
        noise.SetDomainWarpAmp(15f);
        noise.SetFrequency(0.3f);
        noise.SetFractalType(FractalType.DomainWarpIndependent);
        noise.SetFractalOctaves(3);

        var x = 15f;
        var y = 25f;
        noise.DomainWarp(ref x, ref y);

        Assert.Multiple(() =>
        {
            Assert.That(x, Is.Not.EqualTo(15f));
            Assert.That(y, Is.Not.EqualTo(25f));
        });
    }

    [Test]
    public void DomainWarp3D_ShouldModifyCoordinates()
    {
        var noise = new FastNoiseLite();
        noise.SetDomainWarpType(DomainWarpType.OpenSimplex2);
        noise.SetDomainWarpAmp(12f);
        noise.SetFrequency(0.25f);
        noise.SetFractalType(FractalType.DomainWarpProgressive);
        noise.SetFractalOctaves(2);

        var x = 3f;
        var y = 4f;
        var z = 5f;
        noise.DomainWarp(ref x, ref y, ref z);

        Assert.Multiple(() =>
        {
            Assert.That(x, Is.Not.EqualTo(3f));
            Assert.That(y, Is.Not.EqualTo(4f));
            Assert.That(z, Is.Not.EqualTo(5f));
        });
    }
}
