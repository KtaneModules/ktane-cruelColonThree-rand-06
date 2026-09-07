using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using KModkit;
using UnityEngine;

public class cruelColon3Module : MonoBehaviour
{

	public TextMesh activationsText, timerText, currentPointsText, goalPointsText;
	public KMSelectable colonThreeButton, colonBracketButton, angyColonThreeButton, angyColonBracketButton;
	public KMAudio Audio;
	public KMBombModule Module;
	public MeshRenderer[] ledRenderers;
	public KMBombInfo BombInfo;
	public AudioClip warnSound, pressSound, solveSound;
	
	int activationsCount;
	private int timer = 600;
	private bool canInput;
	private int currentPoints;
	private int goalPoints;
	bool ZenModeActive;
	private bool ModuleSolved;
	private bool TwitchPlaysActive;

	private bool selected;
	private string currentAnswer = "";

	private int A, B, C, D;

	bool isPrime(int num) => Enumerable.Range(2, (int)Mathf.Sqrt(num) - 1).All(x => num % x != 0);

	int getNextPrime(int num)
	{
		do num++; while(!isPrime(num));
		return num;
	}

	void getValues()
	{
		KMBombInfo info = GetComponent<KMBombInfo>();
		A = info.GetSerialNumberNumbers().Sum();
		if (A < 3) A = 3;
		B = info.GetBatteryCount() + info.GetBatteryHolderCount();
		if (B < 3) B = 3;
		if (B == A) B = getNextPrime(B);
		C = info.GetOffIndicators().Count() + 3 * info.GetOnIndicators().Count();
		if (C < 3) C = 3;
		while (C == A || C == B) C = getNextPrime(C);
		D = info.GetPortPlateCount() + info.GetPortPlates().Select(plate => plate.Select(port =>
		{
			switch (port)
			{
				case "DVI": return 6;
				case "PS2": return 5;
				case "Serial": return 4;
				case "RJ45": return 3;
				case "Parallel": return 2;
				case "StereoRCA": return 1;
				default: return 0;
			}
		}).Sum()).Sum();
		if (D < 3) D = 3;
		while (D == A || D == B || D == C) D = getNextPrime(D);
	}
	
	void Start ()
	{
		getValues();
		currentPoints = UnityEngine.Random.Range(-99, -39);
		GetComponent<KMSelectable>().OnFocus += delegate { selected = true;};
		GetComponent<KMSelectable>().OnDefocus += delegate { selected = false;};
		colonThreeButton.OnInteract += delegate { HandlePress(); return false; };
		colonBracketButton.OnInteract += delegate { HandlePress(); return false; };
		angyColonThreeButton.OnInteract += delegate { HandlePress(); return false; };
		angyColonBracketButton.OnInteract += delegate { HandlePress(); return false; };
		GetComponent<KMBombModule>().OnActivate += delegate
		{
			
			goalPoints = ZenModeActive ? 6000 : (int)(BombInfo.GetTime() * 10);
			foreach (MeshRenderer ledRenderer in ledRenderers) ledRenderer.material.color = Color.black;

			goalPointsText.text = goalPoints.ToString();
			activationsText.text = "";
			timerText.text = timer.ToString();
			currentPointsText.text = currentPoints.ToString();
			StartCoroutine(TimerRoutine());
		};
	}
	
	IEnumerator TimerRoutine()
	{
		float lastBombTime = BombInfo.GetTime();

		while (!ModuleSolved)
		{
			float currentBombTime = BombInfo.GetTime();
			if (Mathf.Abs(lastBombTime - currentBombTime) >= 0.1f)
			{
				lastBombTime = currentBombTime;
				timer--;
				timerText.text = timer.ToString();
				if (timer <= 0)
				{
					if (canInput)
					{
						Module.HandleStrike();
						currentPoints -= 200;
						currentPointsText.text = currentPoints.ToString();
					}
					else {Audio.HandlePlaySoundAtTransform(warnSound.name, transform);}
					canInput = !canInput;
					SetTimer(TwitchPlaysActive?900:500);
				}
			}
			yield return null;
		}
	}
	
	void HandlePress()
	{
		if (canInput || ModuleSolved) return;
		timer = timer * 2 > 9999? 9999 : timer * 2;
	}

	string getAnswer(int num) // yandere dev simulator
	{
		string ans = "";
		if (num % (A * A) == 0) ans += "<";
		else if (num % A == 0) ans += ">";
		if (num % 67 == 0)
		{
			if (num % (B * B) == 0) ans += "xX";
			else if (num % B == 0) ans += ";;";
			else ans += "::";
		}
		else
		{
			if (num % (B * B) == 0) ans += "X";
			else if (num % B == 0) ans += ";";
			else ans += ":";
		}
		if (num % (A + B + C + D) == 0) ans += "O";
		if (num % (C * C * (C + 1)) == 0) ans += ")";
		else if (num % (C * C * C) == 0) ans += ">";
		else if (num % (C * (C + 1)) == 0) ans += "}";
		else if (num % (C * C) == 0) ans += "|";
		else if (num % C == 0) ans += "]";
		else ans += "3";
		if (num % (D * D) == 0) ans += "C";
		else if (num % D == 0) ans += "c";
		return ans;
	}

	string getReversedAnswer(string str) => str.Any(c => "cC3".Contains(c)) ? "" : 
		str.Reverse().Select(c => "><xX;:O({|["["<>xX;:O)}|]".IndexOf(c)].ToString()).Aggregate("", (a, b) => a + b);
	
	
	void submit(string str)
	{
		canInput = false;
		string ans = getAnswer(currentPoints);
		string rev = getReversedAnswer(str);
		activationsText.text = "";
		if (str != rev && str != ans)
		{
			Module.HandleStrike();
			currentPoints -= 127;
		}
		if (str == rev)
		{
			currentPoints += 2 * timer;
			Audio.HandlePlaySoundAtTransform(pressSound.name, transform);
		}
		if (str == ans)
		{
			currentPoints += timer;
			Audio.HandlePlaySoundAtTransform(pressSound.name, transform);
		}
		currentPointsText.text = currentPoints.ToString();
		SetTimer(timer + 150);
		if (currentPoints < goalPoints) return;
		foreach (MeshRenderer ledRenderer in ledRenderers)
			ledRenderer.material.color = Color.green;
		Module.HandlePass();
		Audio.HandlePlaySoundAtTransform(solveSound.name, transform);
		ModuleSolved = true;
		timerText.text = ":3c";
	}
	
	void SetTimer(int value)
	{
		if (!canInput) value = Mathf.Min(value, goalPoints/2);
		timer = value;
		timerText.text = timer.ToString();
		foreach (MeshRenderer ledRenderer in ledRenderers)
			ledRenderer.material.color = canInput ? Color.red : Color.black;
	}

	char checkInput()
	{
		// check for <>xX;:(){}|[]3CcO
		// being: x[];c  \90o,.  3
		//        X{}:C  |()O<>
		if (Input.GetKeyDown(KeyCode.Backspace)) return 'd';
		if (Input.GetKeyDown(KeyCode.Return)) return 'r';
		if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
		{
			if (Input.GetKeyDown(KeyCode.X)) return 'X';
			if (Input.GetKeyDown(KeyCode.LeftBracket)) return '{';
			if (Input.GetKeyDown(KeyCode.RightBracket)) return '}';
			if (Input.GetKeyDown(KeyCode.Semicolon)) return ':';
			if (Input.GetKeyDown(KeyCode.C)) return 'C';
			
			if (Input.GetKeyDown(KeyCode.Backslash)) return '|';
			if (Input.GetKeyDown(KeyCode.Alpha9)) return '(';
			if (Input.GetKeyDown(KeyCode.Alpha0)) return ')';
			if (Input.GetKeyDown(KeyCode.O))  return 'O';
			if (Input.GetKeyDown(KeyCode.Comma)) return '<';
			if (Input.GetKeyDown(KeyCode.Period)) return '>';
		}
		else
		{
			if (Input.GetKeyDown(KeyCode.X)) return 'x';
			if (Input.GetKeyDown(KeyCode.LeftBracket)) return '[';
			if (Input.GetKeyDown(KeyCode.RightBracket)) return ']';
			if (Input.GetKeyDown(KeyCode.Semicolon)) return ';';
			if (Input.GetKeyDown(KeyCode.C)) return 'c';
			
			if (Input.GetKeyDown(KeyCode.Alpha3) ||  Input.GetKeyDown(KeyCode.Keypad3)) return '3';
		}

		return '\0';
	}

	void processInput(char c)
	{
		if (c=='\0' || !canInput) return;
		if (c=='d' && currentAnswer!="")
		{
			currentAnswer = currentAnswer.Substring(0, currentAnswer.Length - 1);
			activationsText.text = currentAnswer;
			return;
		}
		if (c == 'r' && currentAnswer != "")
		{
			submit(currentAnswer);
			return;
		}
		if (currentAnswer.Length < 6) currentAnswer += c;
		activationsText.text = currentAnswer;
	}

	void Update()
	{
		if (!ModuleSolved && selected) processInput(checkInput());
		if (ModuleSolved || (BombInfo.GetTime() == 0 && !ZenModeActive) || ZenModeActive) return;
		goalPoints = (int)(BombInfo.GetTime() * 10);
		goalPointsText.text = goalPoints.ToString();
	}
	
#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"Use !{0} <emoticon> to submit emoticon. MAKE SURE YOU DON'T MESS UP THE LETTER CASES!";
#pragma warning restore 414
	
	public IEnumerator ProcessTwitchCommand(string Command)
	{
		yield return null;
		submit(Command);
	}

	public IEnumerator TwitchHandleForcedSolve()
	{
		yield return null;
		goalPointsText.text = "0";
		ModuleSolved = true;
		Module.HandlePass();
	}
}
