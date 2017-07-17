using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net;
using System.IO;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using SimpleJSON;
using System.Threading;

public class ZombieScript : MonoBehaviour {

	private string availableDevice;
	public Button recordingControl;
	private AudioSource aud;
	string witToken = "3RXTPB5FOQPIUWLIX2NBDVZN4N4IGYCG";
	bool stop;
	Thread thread;
	Animation anim;

	// Use this for initialization
	void Start () {
		Debug.Log ("VOICE CONTROL!");
		aud = this.GetComponent<AudioSource> ();
		thread = new Thread (Run);

		if (Microphone.devices.Length > 0) {
			availableDevice = Microphone.devices [0];

			Button btn = recordingControl.GetComponent<Button> ();
			btn.onClick.AddListener (TaskOnClick);
		} else {
			Debug.Log ("No mics available");
		}
		thread.Start ();
	}
	
	// Update is called once per frame
	void Update () {

	}

	void Run(){
		while(!stop){
			string witAIResponse = GetJSONText("./storage/emulated/0/Android/data/com.vibaza.voicecontrol/files/speechCommand.wav");
			Debug.Log ("RESPONSE: " + witAIResponse);
			PlayAnimation (witAIResponse);
		}
	}

	void TaskOnClick(){ 

		if (!Microphone.IsRecording (availableDevice)) {
			recordingControl.GetComponent<Image> ().color = Color.green;
			aud.clip = Microphone.Start (availableDevice, false, 10, 16000);
		} else if (Microphone.IsRecording (availableDevice)) {			
			recordingControl.GetComponent<Image> ().color = Color.red;
			Microphone.End (availableDevice);
			SavWav.Save ("speechCommand", aud.clip);
			aud.clip = null;
		}
	}

	string GetJSONText(string file) {
		ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
		// get the file w/ FileStream
		FileStream filestream = new FileStream (file, FileMode.Open, FileAccess.Read);
		BinaryReader filereader = new BinaryReader (filestream);
		byte[] BA_AudioFile = filereader.ReadBytes ((Int32)filestream.Length);
		filestream.Close ();
		filereader.Close ();

		// create an HttpWebRequest
		HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://api.wit.ai/speech");

		request.Method = "POST";
		request.Headers ["Authorization"] = "Bearer " + witToken;
		request.ContentType = "audio/wav";
		request.ContentLength = BA_AudioFile.Length;
		request.GetRequestStream ().Write (BA_AudioFile, 0, BA_AudioFile.Length);

		// Process the wit.ai response
		try
		{
			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			if (response.StatusCode == HttpStatusCode.OK)
			{
				print("Http went through ok");
				StreamReader response_stream = new StreamReader(response.GetResponseStream());
				return response_stream.ReadToEnd();
				Debug.Log(response_stream.ReadToEnd());
			}
			else
			{
				return "Error: " + response.StatusCode.ToString();
				return "HTTP ERROR";
			}
		}
		catch (Exception ex)
		{
			return "Error: " + ex.Message;
			return "HTTP ERROR";
		}  
			
	}

	public bool MyRemoteCertificateValidationCallback(System.Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
		bool isOk = true;
		// If there are errors in the certificate chain, look at each error to determine the cause.
		if (sslPolicyErrors != SslPolicyErrors.None) {
			for (int i=0; i<chain.ChainStatus.Length; i++) {
				if (chain.ChainStatus [i].Status != X509ChainStatusFlags.RevocationStatusUnknown) {
					chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
					chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
					chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan (0, 1, 0);
					chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
					bool chainIsValid = chain.Build ((X509Certificate2)certificate);
					if (!chainIsValid) {
						isOk = false;
					}
				}
			}
		}
		return isOk;
	}

	void PlayAnimation(string textToParse) {

		var N = JSON.Parse (textToParse);

		string intent = N["entities"]["play_animation"][0]["value"].Value.ToLower ();
		Debug.Log ("SimpleJSON: " + intent);

		switch (intent){
		case "walk":
			print ("Intent is walk");
			GetComponent<Animation>().PlayQueued("walk", QueueMode.PlayNow);
			break;
		case "right":
			print ("Intent is fall right");
			GetComponent<Animation>().PlayQueued("right_fall", QueueMode.PlayNow);
			break;
		case "left":
			print ("Intent is fall left");
			GetComponent<Animation>().PlayQueued("left_fall", QueueMode.PlayNow);
			break;
		case "back":
			print ("Intent is fall back");
			GetComponent<Animation>().PlayQueued("back_fall", QueueMode.PlayNow);
			break;
		case "attack":
			print ("Intent is attack");
			GetComponent<Animation>().PlayQueued("attack", QueueMode.PlayNow);
			break;
		default:
			print ("Sorry, didn't understand your intent.");
			break;
		}

	}

	void OnApplicationQuit(){
		stop = true;
		thread.Abort ();
	}

	void OnDestroy(){
		stop = true;
		thread.Abort ();
	}

}
