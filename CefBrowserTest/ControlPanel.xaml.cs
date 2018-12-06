using Mixer.Base;
using Mixer.Base.Clients;
using Mixer.Base.Model.Channel;
using Mixer.Base.Model.Interactive;
using Mixer.Base.Model.User;
using Mixer.Base.Util;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CefBrowserTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ControlPanel : Window
    {
        private MixerConnection connection;
        private ExpandedChannelModel channel;
        private PrivatePopulatedUserModel user;
        private InteractiveClient interactiveClient;
        private ChatClient chatClient;

        private List<InteractiveGameListingModel> games;
        private InteractiveGameListingModel game;

        private List<InteractiveConnectedSceneModel> scenes = new List<InteractiveConnectedSceneModel>();

        private List<InteractiveConnectedButtonControlModel> buttons = new List<InteractiveConnectedButtonControlModel>();
        private List<InteractiveConnectedJoystickControlModel> joysticks = new List<InteractiveConnectedJoystickControlModel>();
        private List<InteractiveConnectedLabelControlModel> labels = new List<InteractiveConnectedLabelControlModel>();
        private List<InteractiveConnectedTextBoxControlModel> textBoxes = new List<InteractiveConnectedTextBoxControlModel>();

        private MainWindow mainWindow;

        private readonly List<User> users = new List<User>();
        private readonly Dictionary<User, string> usersVotes = new Dictionary<User, string>(); // mapping <username, vote>

        private const string AnswerA = "A", AnswerB = "B", AnswerC = "C", AnswerD = "D";
        private readonly Dictionary<string, int> votesCounters = new Dictionary<string, int>() {{AnswerA, 0}, {AnswerB, 0}, {AnswerC, 0}, {AnswerD, 0}};


        public ControlPanel()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
        }

        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            this.LoginGrid.Visibility = Visibility.Visible;

            this.MainGrid.Visibility = Visibility.Collapsed;

            List<OAuthClientScopeEnum> scopes = new List<OAuthClientScopeEnum>()
            {
                OAuthClientScopeEnum.channel__details__self,
                OAuthClientScopeEnum.channel__update__self,

                OAuthClientScopeEnum.chat__bypass_links,
                OAuthClientScopeEnum.chat__bypass_slowchat,
                OAuthClientScopeEnum.chat__change_ban,
                OAuthClientScopeEnum.chat__change_role,
                OAuthClientScopeEnum.chat__chat,
                OAuthClientScopeEnum.chat__connect,
                OAuthClientScopeEnum.chat__clear_messages,
                OAuthClientScopeEnum.chat__edit_options,
                OAuthClientScopeEnum.chat__giveaway_start,
                OAuthClientScopeEnum.chat__poll_start,
                OAuthClientScopeEnum.chat__poll_vote,
                OAuthClientScopeEnum.chat__purge,
                OAuthClientScopeEnum.chat__remove_message,
                OAuthClientScopeEnum.chat__timeout,
                OAuthClientScopeEnum.chat__view_deleted,
                OAuthClientScopeEnum.chat__whisper,

                OAuthClientScopeEnum.channel__details__self,
                OAuthClientScopeEnum.channel__update__self,

                OAuthClientScopeEnum.user__details__self,
                OAuthClientScopeEnum.user__log__self,
                OAuthClientScopeEnum.user__notification__self,
                OAuthClientScopeEnum.user__update__self,

                OAuthClientScopeEnum.interactive__manage__self,
                OAuthClientScopeEnum.interactive__robot__self,

                OAuthClientScopeEnum.user__details__self,
                OAuthClientScopeEnum.user__log__self,
                OAuthClientScopeEnum.user__notification__self,
                OAuthClientScopeEnum.user__update__self,
            };

            this.connection = await MixerConnection.ConnectViaLocalhostOAuthBrowser(ConfigurationManager.AppSettings["ClientID"], scopes);

            if (this.connection != null)
            {
                this.user = await this.connection.Users.GetCurrentUser();
                this.channel = await this.connection.Channels.GetChannel(this.user.username);

                this.games = new List<InteractiveGameListingModel>(await this.connection.Interactive.GetOwnedInteractiveGames(this.channel));
                this.GameSelectComboBox.ItemsSource = this.games.Select(g => g.name);

                this.LoginGrid.Visibility = Visibility.Collapsed;

                this.GameSelectGrid.Visibility = Visibility.Visible;

                this.chatClient = await ChatClient.CreateFromChannel(this.connection, this.channel);
                if (await this.chatClient.Connect() && await this.chatClient.Authenticate())
                {
                    this.InteractiveDataTextBlock.Text += "Connected to the chat." + Environment.NewLine;
                }
            }   
        }

        private async void GameSelectButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.GameSelectComboBox.SelectedIndex >= 0)
            {
                string gameName = (string)this.GameSelectComboBox.SelectedItem;
                this.game = this.games.FirstOrDefault(g => g.name.Equals(gameName));

                this.interactiveClient = await InteractiveClient.CreateFromChannel(this.connection, this.channel, this.game);

                if (await this.interactiveClient.Connect() && await this.interactiveClient.Ready())
                {
                    this.interactiveClient.OnDisconnectOccurred += InteractiveClient_OnDisconnectOccurred;
                    this.interactiveClient.OnGiveInput += InteractiveClient_OnGiveInput;
                    this.interactiveClient.OnGroupCreate += InteractiveClient_OnGroupCreate;
                    this.interactiveClient.OnGroupDelete += InteractiveClient_OnGroupDelete;
                    this.interactiveClient.OnGroupUpdate += InteractiveClient_OnGroupUpdate;
                    this.interactiveClient.OnIssueMemoryWarning += InteractiveClient_OnIssueMemoryWarning;
                    this.interactiveClient.OnParticipantJoin += InteractiveClient_OnParticipantJoin;
                    this.interactiveClient.OnParticipantLeave += InteractiveClient_OnParticipantLeave;
                    this.interactiveClient.OnParticipantUpdate += InteractiveClient_OnParticipantUpdate;

                    this.GameSelectGrid.Visibility = Visibility.Collapsed;

                    this.MainGrid.Visibility = Visibility.Visible;

                    InteractiveConnectedSceneGroupCollectionModel scenes = await this.interactiveClient.GetScenes();
                    if (scenes != null)
                    {
                        this.scenes = new List<InteractiveConnectedSceneModel>(scenes.scenes);

                        foreach (InteractiveConnectedSceneModel scene in this.scenes)
                        {
                            if (scene.allControls.Count() > 0)
                            {
                                foreach (InteractiveConnectedButtonControlModel button in scene.buttons)
                                {
                                    this.buttons.Add(button);
                                }

                                foreach (InteractiveConnectedJoystickControlModel joystick in scene.joysticks)
                                {
                                    this.joysticks.Add(joystick);
                                }

                                foreach (InteractiveConnectedLabelControlModel label in scene.labels)
                                {
                                    this.labels.Add(label);
                                }

                                foreach (InteractiveConnectedTextBoxControlModel textBox in scene.textBoxes)
                                {
                                    this.textBoxes.Add(textBox);
                                }

                                foreach (InteractiveControlModel control in scene.allControls)
                                {
                                    control.disabled = false;
                                }

                                await this.interactiveClient.UpdateControls(scene, scene.allControls);
                            }
                        }
                    }

                    DateTimeOffset? dateTime = await this.interactiveClient.GetTime();
                }
            }
        }

        private async void MainWindow_Closed(object sender, EventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        #region Event Methods

        private async void InteractiveClient_OnDisconnectOccurred(object sender, System.Net.WebSockets.WebSocketCloseStatus e)
        {
            this.InteractiveDataTextBlock.Text += "Disconnection Occurred, attempting reconnection..." + Environment.NewLine;

            do
            {
                await Task.Delay(2500);
            }
            while (!await this.interactiveClient.Connect() && !await this.interactiveClient.Ready());

            this.InteractiveDataTextBlock.Text += "Reconnection successful" + Environment.NewLine;
        }

        private async void InteractiveClient_OnGiveInput(object sender, InteractiveGiveInputModel e)
        {
            this.InteractiveDataTextBlock.Text += "Input Received: " + e.participantID + " - " + e.input.eventType + " - " + e.input.controlID + Environment.NewLine;

            
            if (e.input.eventType.Equals("mousedown"))
            {
                this.ProcessVote(sender, e);

                if (e.transactionID != null)
                {


                    InteractiveConnectedButtonControlModel button =
                        this.buttons.FirstOrDefault(b => b.controlID.Equals(e.input.controlID));
                    if (button != null)
                    {
                        InteractiveConnectedSceneModel scene =
                            this.scenes.FirstOrDefault(s => s.buttons.Contains(button));
                        if (scene != null)
                        {
                            button.cooldown =
                                DateTimeHelper.DateTimeOffsetToUnixTimestamp(DateTimeOffset.Now.AddSeconds(10));
                            await this.interactiveClient.UpdateControls(scene,
                                new List<InteractiveConnectedButtonControlModel>() {button});
                            this.InteractiveDataTextBlock.Text +=
                                "Sent 10 second cooldown to button: " + e.input.controlID + Environment.NewLine;
                        }
                    }

                    await this.interactiveClient.CaptureSparkTransaction(e.transactionID);
                    this.InteractiveDataTextBlock.Text += "Spark Transaction Captured: " + e.participantID + " - " +
                                                          e.input.eventType + " - " + e.input.controlID +
                                                          Environment.NewLine;
                }
            }
        }

        private async void ProcessVote(object sender, InteractiveGiveInputModel e)
        {
            var existingUser = users.FirstOrDefault(u => u.Sessions.Contains(e.participantID));
            if (existingUser == null) // added to support users who not recorded by Joined event
            {
                var allParticipants = await((InteractiveClient)sender).GetAllParticipants(null);
                var participant = allParticipants.participants.Single(p => p.sessionID.Equals(e.participantID));
                existingUser = new User(participant.username, participant.sessionID);
                this.users.Add(existingUser);
                this.InteractiveDataTextBlock.Text += $"[Info]: Registered player: session id {e.participantID}, user {participant.username}" + Environment.NewLine;
            }

            if (!usersVotes.ContainsKey(existingUser))
            {
                usersVotes[existingUser] = string.Empty;
            }

            if (!usersVotes[existingUser].Equals(e.input.controlID))
            {
                if (!usersVotes[existingUser].Equals(string.Empty)) // very first vote of the session ID for current question
                {
                    this.votesCounters[usersVotes[existingUser]]--;
                }

                this.votesCounters[e.input.controlID]++;
                usersVotes[existingUser] = e.input.controlID;
                await this.chatClient.SendMessage($"{existingUser.Name} votes {e.input.controlID}");
            }

            var statusMessage =
                $"A:{votesCounters[AnswerA]}, B:{votesCounters[AnswerB]}, C:{votesCounters[AnswerC]}, D:{votesCounters[AnswerD]}";

            await this.chatClient.SendMessage(statusMessage);
            this.InteractiveDataTextBlock.Text += statusMessage + Environment.NewLine;
        }

        private void InteractiveClient_OnGroupCreate(object sender, InteractiveGroupCollectionModel e)
        {
            if (e.groups != null)
            {
                foreach (InteractiveGroupModel group in e.groups)
                {
                    this.InteractiveDataTextBlock.Text += "Group Created: " + group.groupID + Environment.NewLine;
                }
            }
        }

        private void InteractiveClient_OnGroupDelete(object sender, Tuple<InteractiveGroupModel, InteractiveGroupModel> e)
        {
            this.InteractiveDataTextBlock.Text += "Group Deleted: " + e.Item1 + " - " + e.Item2 + Environment.NewLine;
        }

        private void InteractiveClient_OnGroupUpdate(object sender, InteractiveGroupCollectionModel e)
        {
            if (e.groups != null)
            {
                foreach (InteractiveGroupModel group in e.groups)
                {
                    this.InteractiveDataTextBlock.Text += "Group Updated: " + group.groupID + Environment.NewLine;
                }
            }
        }

        private void InteractiveClient_OnIssueMemoryWarning(object sender, InteractiveIssueMemoryWarningModel e)
        {
            this.InteractiveDataTextBlock.Text += "Memory Warning Issued: " + e.usedBytes + " bytes used out of " + e.totalBytes + " total bytes" + Environment.NewLine;
        }

        private void InteractiveClient_OnParticipantJoin(object sender, InteractiveParticipantCollectionModel e)
        {
            if (e.participants != null)
            {
                foreach (InteractiveParticipantModel participant in e.participants)
                {
                    this.InteractiveDataTextBlock.Text += $"Participant Joined: {participant.username}, sessionID: {participant.sessionID}" + Environment.NewLine;

                    var newUser = new User(participant.username, participant.sessionID);
                    if (this.users.Contains(newUser))
                    {
                        this.users.First(u => u.Equals(newUser)).Sessions.Add(participant.sessionID);
                    }
                    else
                    {
                        this.users.Add(newUser);
                    }
                }
            }
        }

        private async void ControlPanelWindow_Closed(object sender, EventArgs e)
        {
            if (this.interactiveClient != null)
            {
                await this.interactiveClient.Disconnect();
            }

            if (this.chatClient != null)
            {
                await this.chatClient.Disconnect();   
            }

            System.Windows.Application.Current.Shutdown();
        }

        private void InteractiveClient_OnParticipantLeave(object sender, InteractiveParticipantCollectionModel e)
        {
            if (e.participants != null)
            {
                foreach (InteractiveParticipantModel participant in e.participants)
                {
                    this.InteractiveDataTextBlock.Text += "Participant Left: " + participant.username + Environment.NewLine;

                    var existingUser = users.FirstOrDefault(u => u.Name.Equals(participant.username));
                    if (existingUser != null)
                    {
                        this.users.Remove(existingUser);
                    }   
                }
            }
        }

        private void InteractiveClient_OnParticipantUpdate(object sender, InteractiveParticipantCollectionModel e)
        {
            if (e.participants != null)
            {
                foreach (InteractiveParticipantModel participant in e.participants)
                {
                    this.InteractiveDataTextBlock.Text += "Participant Updated: " + participant.username + Environment.NewLine;
                }
            }
        }

        #endregion Event Methods

        private void LaunchGameButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.mainWindow = new MainWindow();
            this.mainWindow.Show();
        }
    }
}
