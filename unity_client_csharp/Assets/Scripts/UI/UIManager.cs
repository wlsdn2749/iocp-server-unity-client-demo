using UnityEngine;

public enum UIState { Login, Register, Game }
public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject loginUI, registerUI, gameUI;
    
    public static UIManager Instance;
    private UIState current = UIState.Login;
    void Awake()
    {
        Instance = this;
    }
    
    public void SetState(UIState next)
    {
        current = next;
        loginUI   .SetActive(current == UIState.Login);
        registerUI.SetActive(current == UIState.Register);
        gameUI    .SetActive(current == UIState.Game);
    }
}
