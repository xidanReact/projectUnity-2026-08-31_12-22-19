using NUnit.Framework;

/// <summary>
/// Перелистывание патогенов. Арифметика вынесена из экрана, потому что
/// заворачивание на краях — единственное, что здесь можно сломать незаметно.
/// </summary>
public class PathogenCarouselTests
{
    [Test]
    public void Types_СодержитВсеЧетыреПатогена()
    {
        Assert.AreEqual(4, PathogenCarousel.Types.Length);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Virus);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Bacteria);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Fungus);
        CollectionAssert.Contains(PathogenCarousel.Types, PathogenType.Parasite);
    }

    [Test]
    public void Shift_ЛистаетВпередИНазад()
    {
        Assert.AreEqual(1, PathogenCarousel.Shift(0, 1));
        Assert.AreEqual(0, PathogenCarousel.Shift(1, -1));
    }

    [Test]
    public void Shift_ЗаворачиваетсяНаОбоихКраях()
    {
        int last = PathogenCarousel.Types.Length - 1;

        Assert.AreEqual(0, PathogenCarousel.Shift(last, 1), "С последнего вперёд — на первый");
        Assert.AreEqual(last, PathogenCarousel.Shift(0, -1), "С первого назад — на последний");
    }

    [Test]
    public void IndexOf_НаходитПоИмениИПадаетВНольНаМусоре()
    {
        Assert.AreEqual(0, PathogenCarousel.IndexOf("Virus"));
        Assert.AreEqual(2, PathogenCarousel.IndexOf("Fungus"));
        Assert.AreEqual(0, PathogenCarousel.IndexOf("нет_такого"),
            "Испорченный сейв не должен ронять главный экран");
        Assert.AreEqual(0, PathogenCarousel.IndexOf(null));
    }
}
