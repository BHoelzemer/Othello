/**
 *	Author:	Ben Hölzemer
 *	Date:	28.01.2014
 *	Description:
 *	    main class of the Othello Game http://benholz.lima-city.de/Othello.html
 *	    
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Game : MonoBehaviour 
{
    //raycast
    public RaycastHit hit;
    private Ray ray;
    GameObject m_hitCube;
    public LayerMask m_layerMask;//1 << 8; // Token

    public GUISkin m_guiSkin = null;
    public Material m_material;

    System.Random m_random = new System.Random();
    public int m_BoardSize = 10;
    public int m_cellScale = 1;
    public GameObject m_originalToken;
    Cell[,] m_cells;
    int m_turn; // -1 = white 1 = black
    static int turnWatch = m_firstPlayer;
    public const int WHITE = -1;
    public const int BLACK = 1;
    public static GameState m_state = GameState.singleplayer;
    public static int m_firstPlayer;
    public static int m_secondPlayer;
    public static int m_AIPlayer;
    public static int m_animationCounter = 0; // for performance


    enum CellState
    {
        empty = 0,
        black,
        white,

    };

    public enum GameState
    {
        start = 0, 
        multiplayer, // set in the menu
        singleplayer,
        final,
    };

    struct Cell
    {
        public GameObject token;
        public CellState  state;

    };
    

	void Start () 
    {

        // the board
        m_cells = new Cell[10, 10];
	    GameObject board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.transform.position = new Vector3(m_BoardSize / 2, m_BoardSize / 2, 0);
        board.transform.localScale += new Vector3(m_BoardSize - 1, m_BoardSize - 1, 0);
        board.renderer.material = m_material;


        //create all the token
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                m_cells[x, y].token = (GameObject)Instantiate(m_originalToken);
                m_cells[x, y].token.transform.position = new Vector3(x + 0.5f, y + 0.5f, -0.1f);
                m_cells[x, y].token.transform.GetChild(0).renderer.enabled = false;
                m_cells[x, y].state = CellState.empty;
            }
        }
        
        //4 statring position
        SetToken(4, 4, Color.black);
        SetToken(4, 5, Color.white);
        SetToken(5, 4, Color.white);
        SetToken(5, 5, Color.black);


        //Handle menu input
        m_secondPlayer = m_firstPlayer * -1;
        m_AIPlayer = m_secondPlayer;
        m_turn = WHITE;

        SetCamera();

	}

    void OnGUI()
    {
        GUI.skin = m_guiSkin;
        if (m_state != GameState.final)
        { 
            string text = (m_turn == WHITE) ? "Weiß ist am Zug." : "Schwarz ist am Zug.";
            GUI.TextArea(new Rect(10.0f, 10.0f, 140.0f, 30.0f), text);

        }
        else
        {
            string text = GetWinner() + "\n" + "Drücke hier um ins\n Hauptmenü zurückzukehren";
            if (GUI.Button(new Rect(Screen.width / 2 - 100.0f, Screen.height / 2 - 75.0f, 200.0f, 150.0f), text))
            {
                Application.LoadLevel("Menu");
            }
        }
    }
	void Update () 
    {

        
        // turnWatch is for performance, NextTurnPossible is to intensive
        if (turnWatch != m_turn )
        {
            // can the player put at least one token
            if (!NextTurnPossible())
            {
                m_state = GameState.final;
            } 
            turnWatch = m_turn;
        }

        // all animations have finished
        if (m_animationCounter <= 0)
        {
            switch (m_state)
            {
                case GameState.multiplayer:
                    HandleInput();
                    break;

                case GameState.singleplayer:

                    if (m_turn == m_firstPlayer)
                    {
                        HandleInput();
                    }
                    else
                    {
                        AIPlayer();
                    }
                    break;

                case GameState.final:
                    return;
            }
        }
	}

    /// <summary>
    /// Find positions were Tokens can be set
    /// </summary>
    /// <returns>the positions of up to 8 Lines( all Token that can be fliped)</returns>
    List<List<Vector2>> CanChangeBoard(int _x, int _y)
    {
        // one line of positions
        List<Vector2> tmpList = new List<Vector2>();
        // up to 8 possible lines one for each direction
        List<List<Vector2>> result = new List<List<Vector2>>();
 
        if (m_cells[_x, _y].state > CellState.empty)
        {
            return result;
        }
        // this two loops set the direction, resulting in 8 directions
        for (int i = -1; i < 2; i++)
        {
            for (int j = -1; j < 2; j++)
            {
                tmpList = FindLine(_x, _y, i, j);
                // a valid line is found
                if (tmpList.Count > 0)
                {
                    result.Add(tmpList);
                }
            }
        }
        return result;
    }

    List<Vector2> FindLine(int _x, int _y, int _dirX, int _dirY)
    {
        List<Vector2> list = new List<Vector2>();
        int x = _x + _dirX;
        int y = _y + _dirY;
        int counter = 0;
        // the next cell must contain a different color else return the empty list
        if (!IsInBound(x, y) || 
            m_cells[x, y].state == TurnState() ||
            m_cells[x, y].state <= CellState.empty)
        {
            return list;
        }
        // the statment is unnessessary because the code will return at some point, counter is for safety reasons
        while (counter < 10)
        {
            if (IsInBound(x,y) &&
                m_cells[x, y].state == OppositState()) // next cell is opposite state
            {
                // Add to List of Positions
                list.Add(new Vector2(x, y));
                // Go to next cell
                x += _dirX;
                y += _dirY;
            }
            //
            else if (IsInBound(x, y) && // still on the Board
                     list.Count > 0 &&  // at least one result has been found
                     m_cells[x, y].state == TurnState()) // results are surrounded by the same color
            {
                // This give a good result
                return list;
            }
            else 
            {
                // the conditions did not match return a empty list
                list.Clear();
                return list;
            }
            // prevent a endless loop, most likely unnesserary
            counter++;
        }
        // end of loop without result = failiur
        list.Clear();
        return list; 
        
    }

    /// <summary>
    /// Return true if the position is inside the board array
    /// </summary>
    bool IsInBound(int _x, int _y)
    {
        return _x >= 0 && _x < m_BoardSize && _y >= 0 && _y < m_BoardSize;
    }

    CellState TurnState()
    {
        if (m_turn == WHITE)
        {
            return CellState.white;
        }
        else
        {
            return CellState.black;
        }
    }

    CellState OppositState()
    {
        if (m_turn == WHITE)
        {
            return CellState.black;
        }
        else
        {
            return CellState.white;
        }
    }

    /// <summary>
    /// There is at least one Position at the Board where a Token can be set.
    /// </summary>
    bool NextTurnPossible()
    {
        bool result = false;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                if (CanChangeBoard(x, y).Count > 0)
                {
                    result = true;
                }
            }
        }
        return result;
    }

    string GetWinner()
    {
        int whiteCounter = 0;
        int blackCounter = 0;
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                if (m_cells[x, y].state == CellState.black)
                {
                    blackCounter++;
                }
                else if (m_cells[x, y].state == CellState.white)
                {
                    whiteCounter++;
                }
            }
        }

        return (whiteCounter > blackCounter) ? "Weiß hat Gewonnen" : "Schwarz hat Gewonnen";
    }

    /// <summary>
    /// Get the Count of all Positions to determen the best turn for the AI
    /// </summary>
    /// <param name="_lines">use CanChangeBoard() as input</param>
    int CountPositions(List<List<Vector2>> _lines)
    {
        int result = 0;
        foreach (List<Vector2> line in _lines)
        {
            result += line.Count;
        }

        return result;
    }

    void AIPlayer()
    {
        List<Vector2> startPos = new List<Vector2>();
        List<List<Vector2>> tmpLines = new List<List<Vector2>>();
        List<List<List<Vector2>>> allPossibleMoves = new List<List<List<Vector2>>>();
        
        // search every cell and save its value if possible
        for (int x = 0; x < 10; x++)
        {
            for (int y = 0; y < 10; y++)
            {
                tmpLines = CanChangeBoard(x, y);
                if (CountPositions(tmpLines) > 0)
                {
                    allPossibleMoves.Add(new List<List<Vector2>>(tmpLines));
                    startPos.Add(new Vector2(x,y));
                }
                tmpLines.Clear();
            }
        }

        int counter = 0;
        int tmpCount = 0;
        List<int> counts = new List<int>();
        // find the longest line
        foreach (List<List<Vector2>> item in allPossibleMoves)
        {
           
            tmpCount = CountPositions(item);
            if (tmpCount > counter)
            {
                counter = tmpCount;
                // create a sorted list with all count
                counts.Add(counter);
            }
            tmpCount = 0;
        }
        // the Level of the AI/ implement input later
        counter = AIDifficulty(counts, 1);

        // find the Position which count is equal to the diterment count
        for (int i = 0; i < allPossibleMoves.Count; i++)
		{
			if (CountPositions(allPossibleMoves[i]) == counter && counter > 0)
            {
                SetToken((int)startPos[i].x, (int)startPos[i].y, m_AIPlayer);
                FlipAllLines(allPossibleMoves[i]);

                //toggle turns
                m_turn *= -1;
                return;

            }
		}   
    }


    void FlipAllLines(List<List<Vector2>> _lines)
    {
        foreach (List<Vector2> line in _lines)
        {
            foreach (Vector2 pos in line)
            {
                FlipToken((int)pos.x, (int)pos.y);
            }
        }
    }


    void HandleInput()
    {
        //Mouseclick
        if (Input.GetButtonDown("Fire1"))
        {
            //Raycast to get a Token
            ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out hit, m_layerMask))
            {
                m_hitCube = hit.transform.gameObject;
            }
            // iterate to every cell in board
            bool foundMatch = false;
            List<List<Vector2>> tmpLines = new List<List<Vector2>>();
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    tmpLines = CanChangeBoard(x, y);
                    // if the cell is equal to the object from the raycast and lines are found
                    if (m_cells[x, y].token == m_hitCube && tmpLines.Count > 0)
                    {
                        SetToken(x, y, m_turn);
                        FlipAllLines(tmpLines);
                        foundMatch = true;
                    }
                    tmpLines.Clear();
                }
                // Next turn is initialist/prepeart if possible
                if (foundMatch)
                {
                    m_turn *= -1; //toggle
                    m_hitCube = null;
                    break;
                }
            }
        }
    }
    /// <summary>
    /// Handle different resolutions
    /// </summary>
    void SetCamera()
    {
        //set camera to right position
        Camera.main.transform.position = new Vector3(m_BoardSize / 2, m_BoardSize / 2, -5);
        if (Screen.height > Screen.width)
        {
            Camera.main.orthographicSize = (m_BoardSize / 2) * ((float)Screen.height / Screen.width);
        }
        else
        {
            Camera.main.orthographicSize = (m_BoardSize / 2);
        }
    }

    /// <param name="_counts">all count from the Token that can be swaped by setting your Token</param>
    /// <param name="_level"> 1 = low until 3 = high</param>
    int AIDifficulty(List<int> _counts, int _level = 1)
    {
        //_counts is sorted
        //select a range, multiplying with _level gets a larger part of the array
        return _counts[m_random.Next(_counts.Count / 3 * _level)];
    }

    void SetToken(int _x, int _y, Color _col)
    {
        m_cells[_x, _y].token.transform.GetChild(0).renderer.enabled = true;
        m_cells[_x, _y].token.transform.GetChild(0).renderer.material.color = _col;
        if (_col == Color.black)
        {
            m_cells[_x, _y].state = CellState.black;
        }
        else
        {
            m_cells[_x, _y].state = CellState.white;
        }
    }

    void SetToken(int _x, int _y, int _turn)
    {
        m_cells[_x, _y].token.transform.GetChild(0).renderer.enabled = true;
        if (_turn == WHITE)
        {
            m_cells[_x, _y].token.transform.GetChild(0).renderer.material.color = Color.white;
            m_cells[_x, _y].state = CellState.white;
        }
        else
        {
            m_cells[_x, _y].token.transform.GetChild(0).renderer.material.color = Color.black;
            m_cells[_x, _y].state = CellState.black;
        }
    }

    void FlipToken(int _x, int _y)
    {
        // there is a token
        if (m_cells[_x, _y].state > CellState.empty)
        {
            // initiate flip
            m_cells[_x, _y].token.GetComponentInChildren<Flip>().m_performFilp = true;
            //toggle state
            if (m_cells[_x, _y].state == CellState.black)
            {
                m_cells[_x, _y].state = CellState.white;
            }
            else
            {
                m_cells[_x, _y].state = CellState.black;
            }
            // m_animationCounter is set back in the TestFlip script
            m_animationCounter++;
        }
    }

    void CreateToken(int _x, int _y)
    {
        if (m_turn == WHITE)
        {
            SetToken(_x, _y, Color.white);
        }

        if (m_turn == BLACK)
        {
            SetToken(_x, _y, Color.black);
        }
    }

}
