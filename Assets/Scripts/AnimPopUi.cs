using DG.Tweening;
using UnityEngine;

public class AnimPopUi : MonoBehaviour
{
    public void trigger()
    {
        transform.DOScale(1.2f, 0.5f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            transform.DOScale(1f, 0.2f).SetEase(Ease.InOutSine);
        });
    }
}
