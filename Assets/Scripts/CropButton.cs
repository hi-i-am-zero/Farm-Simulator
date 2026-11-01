using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class CropButton : MonoBehaviour
{
    [SerializeField] CropData crop;

    void Start()
    {
        GetComponent<Button>().onClick.AddListener(() =>
        {
            GameManager.Instance.SelectCrop(crop);
            AudioManager.Instance?.Play("click");
        });
    }
}
