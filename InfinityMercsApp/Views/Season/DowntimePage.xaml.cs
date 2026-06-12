using InfinityMercsApp.Services.Season;

namespace InfinityMercsApp.Views.Season;

public partial class DowntimePage : ContentPage, IQueryAttributable
{
    private string _companyFilePath = string.Empty;
    private string _seasonFilePath = string.Empty;
    private bool _eventResolved;
    private Action<int>? _pendingRollHandler;

    public DowntimePage()
    {
        InitializeComponent();

        var picks = new List<string>();
        for (var i = 1; i <= 20; i++) picks.Add(i.ToString());
        RealPicker.ItemsSource = picks;

        ArmPendingRoll("Press ROLL for a random event, or pick a value and press REAL.", StartEvent);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
        if (query.TryGetValue("seasonFilePath", out var seasonRaw))
            _seasonFilePath = Uri.UnescapeDataString(seasonRaw?.ToString() ?? string.Empty);
    }

    // ── Bottom bar dispatch ──────────────────────────────────────────────────

    private void OnRollClicked(object sender, EventArgs e)
        => DispatchRoll(Random.Shared.Next(1, 21));

    private void OnRealClicked(object sender, EventArgs e)
    {
        if (RealPicker.SelectedIndex < 0)
        {
            DisplayAlert("Pick a number", "Choose a value 1-20 first.", "OK");
            return;
        }
        DispatchRoll(RealPicker.SelectedIndex + 1);
    }

    private void DispatchRoll(int roll)
    {
        if (_pendingRollHandler is null) return;
        var handler = _pendingRollHandler;
        _pendingRollHandler = null;
        SetBarEnabled(false);
        handler(roll);
    }

    private void ArmPendingRoll(string label, Action<int> handler)
    {
        _pendingRollHandler = handler;
        RollContextLabel.Text = label;
        SetBarEnabled(true);
    }

    private void DisarmPendingRoll(string label)
    {
        _pendingRollHandler = null;
        RollContextLabel.Text = label;
        SetBarEnabled(false);
    }

    private void SetBarEnabled(bool enabled)
    {
        RollButton.IsEnabled = enabled;
        RealButton.IsEnabled = enabled;
        RealPicker.IsEnabled = enabled;
        RollBar.Opacity = enabled ? 1.0 : 0.45;
    }

    private async void OnBackToBaseClicked(object sender, EventArgs e)
    {
        await Shell.Current.Navigation.PopToRootAsync();
    }

    // ── Event flow ───────────────────────────────────────────────────────────

    private void StartEvent(int roll)
    {
        EmptyLabel.IsVisible = false;
        EventStack.Children.Clear();

        var evt = DowntimeEventTable.LookupByRoll(roll);
        EventStack.Children.Add(BuildRollHeader(roll, evt));

        _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.EventId = evt.RollRange;
            round.Downtime.Result = evt.Description;
        });

        if (evt.IsNoIncident)
        {
            EventStack.Children.Add(new Label
            {
                Text = evt.Description,
                TextColor = Color.FromArgb("#D1D5DB"),
                FontSize = 14,
                Margin = new Thickness(0, 8, 0, 4)
            });
            ScrollToBottom();
            EventStack.Children.Add(new Label
            {
                Text = "SELECT AN EVENT",
                TextColor = Color.FromArgb("#9CA3AF"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold,
                Margin = new Thickness(0, 4, 0, 6)
            });
            DisarmPendingRoll("Select an event or choose No Event below.");

            foreach (var option in DowntimeEventTable.AllEvents)
            {
                var capturedOption = option;
                var row = BuildEventPickRow(option, () =>
                {
                    // Clear the picker section and run the chosen event
                    EventStack.Children.Clear();
                    ArmPendingRoll("Press ROLL for a random event, or pick a value and press REAL.", StartEvent);
                    StartEvent(capturedOption.RepresentativeRoll);
                });
                EventStack.Children.Add(row);
            }

            var noEventBtn = new Button
            {
                Text = "NO EVENT",
                BackgroundColor = Color.FromArgb("#374151"),
                TextColor = Color.FromArgb("#9CA3AF"),
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 6,
                HeightRequest = 40,
                Margin = new Thickness(0, 8, 0, 0)
            };
            noEventBtn.Clicked += (_, _) =>
            {
                noEventBtn.IsEnabled = false;
                // Grey out remaining rows
                foreach (var child in EventStack.Children.OfType<VisualElement>())
                    if (!ReferenceEquals(child, noEventBtn)) child.Opacity = 0.35;
                DisarmPendingRoll("No event chosen.");

                _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
                {
                    round.Downtime.ChosenPlan = "No event";
                    round.Downtime.Result = "No event chosen";
                });

                ResolveEvent();
            };
            EventStack.Children.Add(noEventBtn);
            ScrollToBottom();
            return;
        }

        EventStack.Children.Add(new Label
        {
            Text = evt.Description,
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 8)
        });

        if (evt.EventTraits.Count > 0)
        {
            EventStack.Children.Add(new Label
            {
                Text = "Event Traits: " + string.Join(", ", evt.EventTraits),
                TextColor = Color.FromArgb("#F59E0B"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });
        }

        // Add a card for each of the three choices
        var choiceCards = new List<Border>();
        for (var i = 0; i < evt.Choices.Count; i++)
        {
            var idx = i;
            var card = BuildChoiceCard(evt, evt.Choices[i], i + 1, () => OnChoiceSelected(evt, idx, choiceCards));
            choiceCards.Add(card);
            EventStack.Children.Add(card);
        }

        DisarmPendingRoll("Pick a choice above to continue.");
        ScrollToBottom();
    }

    private static Border BuildEventPickRow(DowntimeEvent option, Action onPicked)
    {
        var label = new Label
        {
            Text = $"{option.RollRange}  —  {option.Description}",
            TextColor = Colors.White,
            FontSize = 13,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#1A2332"),
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 10, 12, 10),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
            Content = label
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            border.GestureRecognizers.Remove(tap);
            onPicked();
        };
        border.GestureRecognizers.Add(tap);
        return border;
    }

    private View BuildRollHeader(int roll, DowntimeEvent evt)
    {
        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#1F2937"),
            Stroke = Color.FromArgb("#22C55E"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 8, 12, 8),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 }
        };
        border.Content = new Label
        {
            Text = $"Rolled {roll}  →  Event {evt.RollRange}",
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            FontSize = 15,
            HorizontalTextAlignment = TextAlignment.Center
        };
        return border;
    }

    private Border BuildChoiceCard(DowntimeEvent evt, DowntimeChoice choice, int choiceNumber, Action onPicked)
    {
        var stack = new VerticalStackLayout { Spacing = 6, Padding = new Thickness(12) };

        var header = new Label
        {
            Text = $"CHOICE {choiceNumber}{(string.IsNullOrEmpty(choice.TestLabel) ? "" : $"  ({choice.TestLabel})")}",
            TextColor = Color.FromArgb("#9CA3AF"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        };
        stack.Children.Add(header);

        stack.Children.Add(new Label
        {
            Text = choice.Description,
            TextColor = Colors.White,
            FontSize = 14
        });

        if (choice.Traits.Count > 0)
        {
            stack.Children.Add(new Label
            {
                Text = "Traits: " + string.Join(", ", choice.Traits.Select(t => t.Display)),
                TextColor = Color.FromArgb("#F59E0B"),
                FontSize = 12,
                FontAttributes = FontAttributes.Bold
            });
        }

        var pickBtn = new Button
        {
            Text = "PICK",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Colors.White,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 6,
            HeightRequest = 36,
            Margin = new Thickness(0, 4, 0, 0)
        };
        pickBtn.Clicked += (_, _) =>
        {
            pickBtn.IsEnabled = false;
            onPicked();
        };
        stack.Children.Add(pickBtn);

        var border = new Border
        {
            BackgroundColor = Color.FromArgb("#1A2332"),
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            Padding = new Thickness(0),
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 8 }
        };
        border.Content = stack;
        return border;
    }

    private void OnChoiceSelected(DowntimeEvent evt, int choiceIndex, List<Border> choiceCards)
    {
        for (var i = 0; i < choiceCards.Count; i++)
        {
            if (i != choiceIndex)
                EventStack.Children.Remove(choiceCards[i]);
        }

        var choice = evt.Choices[choiceIndex];

        _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.Downtime.ChosenPlan = $"Choice {choiceIndex + 1}: {choice.Description}";
        });

        EventStack.Children.Add(BuildResolutionSection(evt, choice));
        ScrollToBottom();
    }


    private View BuildResolutionSection(DowntimeEvent evt, DowntimeChoice choice)
    {
        var stack = new VerticalStackLayout { Spacing = 10, Margin = new Thickness(0, 8, 0, 0) };

        stack.Children.Add(new Label
        {
            Text = "RESOLUTION",
            TextColor = Color.FromArgb("#9CA3AF"),
            FontSize = 12,
            FontAttributes = FontAttributes.Bold
        });

        // Step 1: Determine the participant who performs the test, if applicable
        var participantArea = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(participantArea);

        // Step 2: Pass/Fail (when applicable)
        var rollArea = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(rollArea);

        // Step 3: Outcomes
        var outcomeArea = new VerticalStackLayout { Spacing = 6 };
        stack.Children.Add(outcomeArea);

        var requiredParticipant = ResolveRequiredParticipantKind(evt, choice);
        var availableUnits = ExperiencePageData.Units;

        ExperienceUnitResult? selectedParticipant = null;
        var crSpentTotal = 0;

        void ShowOutcomes(PassFailOutcome outcome)
        {
            outcomeArea.Children.Clear();

            outcomeArea.Children.Add(new Label
            {
                Text = $"OUTCOME: {OutcomeLabel(outcome)}",
                TextColor = outcome switch
                {
                    PassFailOutcome.Fail => Color.FromArgb("#DC2626"),
                    PassFailOutcome.Pass => Color.FromArgb("#22C55E"),
                    PassFailOutcome.CritPass => Color.FromArgb("#FBBF24"),
                    _ => Colors.White
                },
                FontAttributes = FontAttributes.Bold,
                FontSize = 14
            });

            var consequences = BuildConsequenceLines(choice, outcome, selectedParticipant);
            if (consequences.Count == 0)
            {
                outcomeArea.Children.Add(new Label
                {
                    Text = "No mechanical effect.",
                    TextColor = Color.FromArgb("#9CA3AF"),
                    FontSize = 13
                });
            }
            else
            {
                foreach (var line in consequences)
                {
                    outcomeArea.Children.Add(new Label
                    {
                        Text = "• " + line,
                        TextColor = Color.FromArgb("#D1D5DB"),
                        FontSize = 13
                    });
                }
            }

            ApplyDataMutations(choice, outcome, selectedParticipant);

            var totals = ComputeDowntimeTotals(choice, outcome, crSpentTotal);
            var participantName = selectedParticipant?.Name;
            _ = SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
            {
                round.Downtime.Result = $"{OutcomeLabel(outcome)}{(participantName is null ? "" : $" — {participantName}")}";
                round.Downtime.CrGain = totals.CrGain;
                round.Downtime.NotorietyGain = totals.NotorietyGain;
                round.Downtime.XpGain = totals.XpGain;
                round.Downtime.SpentCr = totals.SpentCr;
                round.Downtime.SwcGain = totals.SwcGain;
                round.Downtime.OtherEffects = string.Join("; ", totals.OtherEffects);
            });

            ScrollToBottom();
            ResolveEvent();
        }

        void ShowRollControls(int crSpent)
        {
            crSpentTotal = crSpent;
            rollArea.Children.Clear();

            var test = choice.Test;
            if (test is null)
            {
                ShowOutcomes(PassFailOutcome.Pass);
                return;
            }

            var baseTarget = ResolveTestTarget(test, selectedParticipant);
            if (!baseTarget.HasValue)
            {
                rollArea.Children.Add(new Label
                {
                    Text = $"Test: {test.Display} — target could not be resolved.",
                    TextColor = Color.FromArgb("#DC2626"),
                    FontSize = 13
                });
                DisarmPendingRoll("Unable to resolve test target.");
                return;
            }

            var effectiveTarget = baseTarget.Value + crSpent;
            var critHint = effectiveTarget > 20
                ? $"  (crit on 20 and 1–{effectiveTarget - 20})"
                : string.Empty;

            rollArea.Children.Add(new Label
            {
                Text = $"Test: {test.Display}  target {effectiveTarget}{critHint}",
                TextColor = Color.FromArgb("#D1D5DB"),
                FontSize = 13
            });

            ArmPendingRoll(
                $"Roll for {test.Display} — target {effectiveTarget}.",
                rolled =>
                {
                    var outcome = ClassifyRoll(rolled, effectiveTarget);
                    rollArea.Children.Add(new Label
                    {
                        Text = $"Rolled: {rolled} vs {effectiveTarget}",
                        TextColor = Colors.White,
                        FontSize = 13,
                        FontAttributes = FontAttributes.Bold
                    });
                    ScrollToBottom();
                    ShowOutcomes(outcome);
                });
            ScrollToBottom();
        }

        void ShowP2PControls(int baseTarget)
        {
            rollArea.Children.Clear();

            rollArea.Children.Add(new Label
            {
                Text = "P2P — Spend CR to boost target (optional):",
                TextColor = Color.FromArgb("#D1D5DB"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });

            var targetLabel = new Label
            {
                Text = $"Target: {baseTarget}",
                TextColor = Color.FromArgb("#22C55E"),
                FontSize = 14,
                FontAttributes = FontAttributes.Bold
            };

            var stepper = new Stepper
            {
                Minimum = 0,
                Maximum = 999,
                Increment = 1,
                Value = 0,
                HorizontalOptions = LayoutOptions.Fill
            };

            stepper.ValueChanged += (_, e) =>
            {
                var cr = (int)e.NewValue;
                var effective = baseTarget + cr;
                targetLabel.Text = effective > 20
                    ? $"Target: {effective}  (crit on 20 and 1–{effective - 20})"
                    : $"Target: {effective}";
            };

            var stepperRow = new Grid { ColumnDefinitions = new ColumnDefinitionCollection(
                new ColumnDefinition { Width = GridLength.Auto },
                new ColumnDefinition { Width = GridLength.Star },
                new ColumnDefinition { Width = GridLength.Auto }), ColumnSpacing = 8 };

            var crLabel = new Label
            {
                Text = "CR to spend:",
                TextColor = Color.FromArgb("#9CA3AF"),
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center
            };
            Grid.SetColumn(crLabel, 0);
            Grid.SetColumn(stepper, 1);
            Grid.SetColumn(targetLabel, 2);
            stepperRow.Children.Add(crLabel);
            stepperRow.Children.Add(stepper);
            stepperRow.Children.Add(targetLabel);
            rollArea.Children.Add(stepperRow);

            var confirmBtn = new Button
            {
                Text = "CONFIRM SPEND",
                BackgroundColor = Color.FromArgb("#374151"),
                TextColor = Colors.White,
                FontAttributes = FontAttributes.Bold,
                CornerRadius = 6,
                HeightRequest = 36
            };
            confirmBtn.Clicked += (_, _) =>
            {
                var cr = (int)stepper.Value;
                stepper.IsEnabled = false;
                confirmBtn.IsEnabled = false;
                confirmBtn.Opacity = 0.45;
                ShowRollControls(cr);
            };
            rollArea.Children.Add(confirmBtn);
            ScrollToBottom();
        }

        void OnParticipantPicked(ExperienceUnitResult unit)
        {
            selectedParticipant = unit;
            participantArea.Children.Add(new Label
            {
                Text = $"Performed by: {unit.Name}",
                TextColor = Color.FromArgb("#22C55E"),
                FontSize = 13,
                FontAttributes = FontAttributes.Bold
            });
            ScrollToBottom();

            var hasP2P = choice.Traits.Any(t => t.Kind == TraitKind.P2P);
            if (hasP2P)
            {
                var test = choice.Test;
                var baseTarget = test is not null ? ResolveTestTarget(test, unit) ?? 0 : 0;
                ShowP2PControls(baseTarget);
            }
            else
            {
                ShowRollControls(0);
            }
        }

        var requirements = choice.Traits
            .Where(t => t.Kind == TraitKind.Requirement && !string.IsNullOrWhiteSpace(t.Detail))
            .Select(t => t.Detail!)
            .ToList();
        BuildParticipantSelector(participantArea, requiredParticipant, availableUnits, requirements, OnParticipantPicked);

        return stack;
    }

    // ── Participant selection ────────────────────────────────────────────────

    private static ParticipantKind ResolveRequiredParticipantKind(DowntimeEvent evt, DowntimeChoice choice)
    {
        // Event-level participants override choice. Event 13-14 = Renowned; some events specify "Merc" in text.
        if (evt.RequiredParticipant != ParticipantKind.None)
            return evt.RequiredParticipant;

        // A merc always performs the event.
        return ParticipantKind.AnyMerc;
    }

    private void BuildParticipantSelector(
        VerticalStackLayout area,
        ParticipantKind kind,
        IReadOnlyList<ExperienceUnitResult> units,
        IReadOnlyList<string> requirements,
        Action<ExperienceUnitResult> onPicked)
    {
        area.Children.Add(new Label
        {
            Text = $"Select {ParticipantKindLabel(kind)}:",
            TextColor = Color.FromArgb("#D1D5DB"),
            FontSize = 13,
            FontAttributes = FontAttributes.Bold
        });

        var candidates = FilterCandidates(units, kind);
        if (candidates.Count == 0)
        {
            area.Children.Add(new Label
            {
                Text = "No eligible mercs found.",
                TextColor = Color.FromArgb("#DC2626"),
                FontSize = 13
            });
            return;
        }

        var rowList = new VerticalStackLayout { Spacing = 6 };
        area.Children.Add(rowList);
        ScrollToBottom();

        var rowControls = new List<(Border Border, TapGestureRecognizer Tap)>();

        foreach (var unit in candidates)
        {
            var missing = requirements.Where(r => !UnitHasTrait(unit, r)).ToList();
            var eligible = missing.Count == 0;

            var nameLabel = new Label
            {
                Text = kind == ParticipantKind.Renowned ? $"{unit.Name}  ({unit.Renown} renown)" : unit.Name,
                TextColor = Colors.White,
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                VerticalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.TailTruncation
            };

            var missingLabel = new Label
            {
                Text = missing.Count > 0 ? $"Missing: {string.Join(", ", missing)}" : string.Empty,
                TextColor = Color.FromArgb("#DC2626"),
                FontAttributes = FontAttributes.Bold,
                FontSize = 13,
                VerticalTextAlignment = TextAlignment.Center,
                HorizontalTextAlignment = TextAlignment.End,
                IsVisible = missing.Count > 0
            };

            var rowGrid = new Grid { ColumnSpacing = 12 };
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
            rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            Grid.SetColumn(nameLabel, 0);
            Grid.SetColumn(missingLabel, 1);
            rowGrid.Children.Add(nameLabel);
            rowGrid.Children.Add(missingLabel);

            var border = new Border
            {
                BackgroundColor = Color.FromArgb("#374151"),
                Stroke = Color.FromArgb("#4B5563"),
                StrokeThickness = 1,
                Padding = new Thickness(12, 10, 12, 10),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Opacity = eligible ? 1.0 : 0.45,
                Content = rowGrid
            };

            var tap = new TapGestureRecognizer();
            if (eligible)
            {
                var capturedUnit = unit;
                tap.Tapped += (_, _) =>
                {
                    // Disable every row after one is picked
                    foreach (var (b, t) in rowControls)
                    {
                        b.GestureRecognizers.Remove(t);
                        if (!ReferenceEquals(b, border)) b.Opacity = 0.45;
                    }
                    border.Stroke = Color.FromArgb("#22C55E");
                    border.StrokeThickness = 2;
                    onPicked(capturedUnit);
                };
                border.GestureRecognizers.Add(tap);
            }

            rowControls.Add((border, tap));
            rowList.Children.Add(border);
        }
    }

    private static bool UnitHasTrait(ExperienceUnitResult unit, string requirement)
    {
        if (string.IsNullOrWhiteSpace(requirement)) return true;
        var haystack = $"{unit.Skills} {unit.Equipment}";
        return haystack.Contains(requirement, StringComparison.OrdinalIgnoreCase);
    }

    private static List<ExperienceUnitResult> FilterCandidates(IReadOnlyList<ExperienceUnitResult> units, ParticipantKind kind)
    {
        switch (kind)
        {
            case ParticipantKind.Mvp:
                return units.Where(u => u.IsMvp).ToList();
            case ParticipantKind.Captain:
                return units.Where(u => u.IsCaptain).ToList();
            case ParticipantKind.Renowned:
                if (units.Count == 0) return new List<ExperienceUnitResult>();
                var topRenown = units.Max(u => u.Renown);
                return units.Where(u => u.Renown == topRenown).ToList();
            default:
                return units.ToList();
        }
    }

    private static string ParticipantKindLabel(ParticipantKind kind) => kind switch
    {
        ParticipantKind.Mvp => "MVP (from last contract)",
        ParticipantKind.Captain => "the Captain",
        ParticipantKind.Renowned => "the Most Renowned merc",
        _ => "a Merc"
    };

    // ── Stat-test target resolution ──────────────────────────────────────────

    private static int? ResolveTestTarget(DowntimeTest test, ExperienceUnitResult? unit)
    {
        switch (test.Kind)
        {
            case TestKind.FixedPs10:
                return 10;
            case TestKind.FixedPs10PlusArm:
            {
                if (unit is null) return 10;
                return 10 + ParseStat(unit.UnitArm);
            }
            case TestKind.Stat:
            {
                if (unit is null) return null;
                var raw = test.StatName switch
                {
                    "PH" => unit.UnitPh,
                    "BS" => unit.UnitBs,
                    "CC" => unit.UnitCc,
                    "WIP" => unit.UnitWip,
                    _ => "-"
                };
                if (!int.TryParse(raw, out var n)) return null;
                return n + test.Modifier;
            }
            default:
                return null;
        }
    }

    private static int ParseStat(string s) => int.TryParse(s, out var n) ? n : 0;

    private static PassFailOutcome ClassifyRoll(int roll, int target)
    {
        if (target <= 20)
        {
            if (roll == target) return PassFailOutcome.CritPass;
            if (roll < target) return PassFailOutcome.Pass;
            return PassFailOutcome.Fail;
        }
        // Target > 20: all rolls succeed. Crits are roll == 20 OR roll <= (target - 20).
        var critRange = target - 20;
        if (roll == 20 || roll <= critRange) return PassFailOutcome.CritPass;
        return PassFailOutcome.Pass;
    }

    private static string OutcomeLabel(PassFailOutcome o) => o switch
    {
        PassFailOutcome.Fail => "FAIL",
        PassFailOutcome.Pass => "PASS",
        PassFailOutcome.CritPass => "CRITICAL PASS",
        _ => "—"
    };

    // ── Consequences ─────────────────────────────────────────────────────────

    private static List<string> BuildConsequenceLines(DowntimeChoice choice, PassFailOutcome outcome, ExperienceUnitResult? participant)
    {
        var lines = new List<string>();
        var participantName = participant?.Name ?? "selected merc";

        foreach (var trait in choice.Traits)
        {
            // (Neg) means only fail-side of the paired trait counts.
            if (trait.Negated && outcome != PassFailOutcome.Fail) continue;

            switch (trait.Kind)
            {
                case TraitKind.Chaotic:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Lose 1 Notoriety",
                        PassFailOutcome.Pass => "Gain 1 Notoriety",
                        PassFailOutcome.CritPass => "Gain 2 Notoriety",
                        _ => ""
                    });
                    break;
                case TraitKind.Lawful:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Gain 1 Notoriety",
                        PassFailOutcome.Pass => "Lose 1 Notoriety",
                        PassFailOutcome.CritPass => "Lose 2 Notoriety",
                        _ => ""
                    });
                    break;
                case TraitKind.Attack:
                    if (outcome == PassFailOutcome.Fail)
                        lines.Add($"Injury inflicted on {participantName}");
                    break;
                case TraitKind.Cr:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Lose 5 CR",
                        PassFailOutcome.Pass => "Gain 5 CR",
                        PassFailOutcome.CritPass => "Gain 6 CR",
                        _ => ""
                    });
                    break;
                case TraitKind.Xp:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Pass => $"{participantName} gains 2 XP",
                        PassFailOutcome.CritPass => $"{participantName} gains 3 XP",
                        _ => ""
                    });
                    break;
                case TraitKind.Weapon:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Pass => $"Roll a random pistol: {RollPistolName(0)}",
                        PassFailOutcome.CritPass => $"Roll a random pistol (+1 SD): {RollPistolName(1)}",
                        _ => ""
                    });
                    break;
                case TraitKind.Swc:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        lines.Add("Gain 0.5 SWC");
                    break;
                case TraitKind.P2P:
                    lines.Add(outcome switch
                    {
                        PassFailOutcome.Fail => "Spent CR are lost; SR not increased",
                        PassFailOutcome.Pass => "Spent CR are lost; SR increased by 1 per CR spent",
                        PassFailOutcome.CritPass => "Spent CR are kept; SR increased by 1 per CR spent",
                        _ => ""
                    });
                    break;
                case TraitKind.Skill:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        lines.Add($"{participantName} gains skill: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Lt:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        lines.Add("You may select any member of your team to be the new Captain");
                    break;
                case TraitKind.Captain:
                    lines.Add("Captain participation required (see event)");
                    break;
                case TraitKind.Requirement:
                    lines.Add($"Requirement: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Note:
                    if (!string.IsNullOrWhiteSpace(trait.Detail)) lines.Add(trait.Detail!);
                    break;
            }
        }

        return lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToList();
    }

    private static readonly (int Min, int Max, string Name)[] PistolTable =
    [
        (1, 4,   "MULTI Pistol"),
        (5, 8,   "AP Heavy Pistol"),
        (9, 12,  "Breaker Pistol"),
        (13, 16, "Silenced Pistol"),
        (17, 20, "Viral Pistol"),
    ];

    private static string RollPistolName(int sdBonus)
    {
        var roll = Random.Shared.Next(1, 21);
        var entry = PistolTable.First(e => roll >= e.Min && roll <= e.Max);
        var suffix = sdBonus > 0 ? $" (+{sdBonus} SD)" : "";
        return $"{entry.Name}{suffix} (rolled {roll})";
    }

    // ── Downtime totals (persistence) ────────────────────────────────────────

    private readonly record struct DowntimeTotals(int CrGain, int NotorietyGain, int XpGain, int SpentCr, double SwcGain, List<string> OtherEffects);

    private static DowntimeTotals ComputeDowntimeTotals(DowntimeChoice choice, PassFailOutcome outcome, int crSpent)
    {
        var crGain = 0;
        var notorietyGain = 0;
        var xpGain = 0;
        var spentCr = 0;
        var swcGain = 0.0;
        var other = new List<string>();

        foreach (var trait in choice.Traits)
        {
            if (trait.Negated && outcome != PassFailOutcome.Fail) continue;

            switch (trait.Kind)
            {
                case TraitKind.Chaotic:
                    notorietyGain += outcome switch
                    {
                        PassFailOutcome.Fail => -1,
                        PassFailOutcome.Pass => 1,
                        PassFailOutcome.CritPass => 2,
                        _ => 0
                    };
                    break;
                case TraitKind.Lawful:
                    notorietyGain += outcome switch
                    {
                        PassFailOutcome.Fail => 1,
                        PassFailOutcome.Pass => -1,
                        PassFailOutcome.CritPass => -2,
                        _ => 0
                    };
                    break;
                case TraitKind.Cr:
                    crGain += outcome switch
                    {
                        PassFailOutcome.Fail => -5,
                        PassFailOutcome.Pass => 5,
                        PassFailOutcome.CritPass => 6,
                        _ => 0
                    };
                    break;
                case TraitKind.Xp:
                    xpGain += outcome switch
                    {
                        PassFailOutcome.Pass => 2,
                        PassFailOutcome.CritPass => 3,
                        _ => 0
                    };
                    break;
                case TraitKind.P2P:
                    // Pass/CritPass keeps or spends CR depending on outcome; the SR/CR rule is handled
                    // in BuildConsequenceLines. Persist the spend separately.
                    if (outcome == PassFailOutcome.CritPass)
                    {
                        // Spent CR is kept on a crit pass — no net loss.
                    }
                    else
                    {
                        spentCr += crSpent;
                    }
                    break;
                case TraitKind.Weapon:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        other.Add(outcome == PassFailOutcome.CritPass ? "Random pistol (+1 SD)" : "Random pistol");
                    break;
                case TraitKind.Skill:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        other.Add($"Skill awarded: {trait.Detail ?? "(see event)"}");
                    break;
                case TraitKind.Lt:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        other.Add("New Captain may be selected");
                    break;
                case TraitKind.Attack:
                    if (outcome == PassFailOutcome.Fail)
                        other.Add("Participant injured");
                    break;
                case TraitKind.Swc:
                    if (outcome is PassFailOutcome.Pass or PassFailOutcome.CritPass)
                        swcGain += 0.5;
                    break;
                case TraitKind.Note:
                    if (!string.IsNullOrWhiteSpace(trait.Detail)) other.Add(trait.Detail!);
                    break;
            }
        }

        return new DowntimeTotals(crGain, notorietyGain, xpGain, spentCr, swcGain, other);
    }

    // ── Data mutations ───────────────────────────────────────────────────────

    private static void ApplyDataMutations(DowntimeChoice choice, PassFailOutcome outcome, ExperienceUnitResult? participant)
    {
        if (participant is null) return;

        foreach (var trait in choice.Traits)
        {
            if (trait.Negated && outcome != PassFailOutcome.Fail) continue;

            if (trait.Kind == TraitKind.Xp)
            {
                participant.XpData.DowntimeBonusXp += outcome switch
                {
                    PassFailOutcome.Pass => 2,
                    PassFailOutcome.CritPass => 3,
                    _ => 0
                };
            }
            else if (trait.Kind == TraitKind.Attack && outcome == PassFailOutcome.Fail)
            {
                participant.GainedInjury = true;
            }
        }
    }

    // ── Event resolution / footer ────────────────────────────────────────────

    private void ScrollToBottom() =>
        Dispatcher.Dispatch(async () =>
            await EventScrollView.ScrollToAsync(0, double.MaxValue, animated: true));

    private void ResolveEvent()
    {
        _eventResolved = true;
        _pendingRollHandler = null;
        RollBar.IsVisible = false;
        RollContextLabel.IsVisible = false;
        ContinueButton.IsVisible = true;
    }
}

// ── Event data model ────────────────────────────────────────────────────────

public enum ParticipantKind { None, AnyMerc, Mvp, Captain, Renowned }
public enum PassFailOutcome { None, Fail, Pass, CritPass }
public enum TestKind { Stat, FixedPs10, FixedPs10PlusArm }
public enum TraitKind
{
    Chaotic, Lawful, Attack, Cr, Xp, Weapon, Swc, P2P, Skill, Lt,
    Captain, Requirement, Note
}

public sealed class DowntimeTest
{
    public TestKind Kind { get; init; }
    public string StatName { get; init; } = string.Empty; // PH, BS, CC, WIP
    public int Modifier { get; init; }                    // e.g. -10 for "CC-10"
    public string Display { get; init; } = string.Empty;  // "(CC-10)", "(PH)", "(PS=10+ARM)"
    public bool NeedsParticipant => Kind == TestKind.Stat || Kind == TestKind.FixedPs10PlusArm;
}

public sealed class DowntimeTrait
{
    public TraitKind Kind { get; init; }
    public string Display { get; init; } = string.Empty; // "Attack", "CR (Neg)", "Skill (Stealth)"
    public string? Detail { get; init; }                 // skill name, requirement detail, note text
    public bool Negated { get; init; }                   // (Neg) — only fail side applies
}

public sealed class DowntimeChoice
{
    public string TestLabel { get; init; } = string.Empty; // e.g. "CC-10", "PH", "WIP", "PS=10"
    public DowntimeTest? Test { get; init; }
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<DowntimeTrait> Traits { get; init; } = Array.Empty<DowntimeTrait>();
}

public sealed class DowntimeEvent
{
    public string RollRange { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public IReadOnlyList<string> EventTraits { get; init; } = Array.Empty<string>();
    public ParticipantKind RequiredParticipant { get; init; } = ParticipantKind.None;
    public IReadOnlyList<DowntimeChoice> Choices { get; init; } = Array.Empty<DowntimeChoice>();
    public bool IsNoIncident { get; init; }
    // A concrete roll value that maps back to this event bucket (used when the player picks manually).
    public int RepresentativeRoll { get; init; }
}

public static class DowntimeEventTable
{
    public static DowntimeEvent LookupByRoll(int roll)
    {
        return roll switch
        {
            <= 2 => Event1To2,
            <= 4 => Event3To4,
            <= 6 => Event5To6,
            <= 8 => Event7To8,
            <= 10 => Event9To10,
            <= 12 => Event11To12,
            <= 14 => Event13To14,
            <= 16 => Event15To16,
            <= 18 => Event17To18,
            _ => Event19To20,
        };
    }

    // All incident events (excludes 19-20) for manual selection.
    public static IReadOnlyList<DowntimeEvent> AllEvents =>
    [
        Event1To2, Event3To4, Event5To6, Event7To8, Event9To10,
        Event11To12, Event13To14, Event15To16, Event17To18
    ];

    // Helpers ----------------------------------------------------------------

    private static DowntimeTest Stat(string stat, int mod, string display) =>
        new() { Kind = TestKind.Stat, StatName = stat, Modifier = mod, Display = display };
    private static DowntimeTest Ps10() =>
        new() { Kind = TestKind.FixedPs10, Display = "PS=10" };
    private static DowntimeTest Ps10PlusArm() =>
        new() { Kind = TestKind.FixedPs10PlusArm, Display = "PS=10+ARM" };

    private static DowntimeTrait T(TraitKind k, string display, string? detail = null, bool negated = false) =>
        new() { Kind = k, Display = display, Detail = detail, Negated = negated };

    // Events -----------------------------------------------------------------

    private static readonly DowntimeEvent Event1To2 = new()
    {
        RollRange = "1-2",
        RepresentativeRoll = 1,
        Description = "A cop pulls you over for driving erratically...",
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "CC-10",
                Test = Stat("CC", -10, "CC-10"),
                Description = "...you reach for the cop's gun",
                Traits = [T(TraitKind.Attack, "Attack"), T(TraitKind.Weapon, "Weapon"), T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "PH",
                Test = Stat("PH", 0, "PH"),
                Description = "...you floor it and drive away",
                Traits = [T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...you try to talk your way out with money",
                Traits = [T(TraitKind.Lawful, "Lawful"), T(TraitKind.P2P, "P2P")]
            }
        ]
    };

    private static readonly DowntimeEvent Event3To4 = new()
    {
        RollRange = "3-4",
        RepresentativeRoll = 3,
        Description = "An ammo dealer supplies you with faulty ammo and refuses to refund you...",
        EventTraits = ["SWC"],
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "BS",
                Test = Stat("BS", 0, "BS"),
                Description = "...you attack the dealer's crew",
                Traits = [T(TraitKind.Attack, "Attack"), T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "PS=10+ARM",
                Test = Ps10PlusArm(),
                Description = "...you let him shoot you to prove your point",
                Traits = [T(TraitKind.Attack, "Attack")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...intimidate the dealer in giving you an honest refund",
                Traits = [T(TraitKind.Cr, "CR")]
            }
        ]
    };

    private static readonly DowntimeEvent Event5To6 = new()
    {
        RollRange = "5-6",
        RepresentativeRoll = 5,
        Description = "You catch one of your Mercs in romantic cahoots with a rival Merc team member after they met in the most recent contract...",
        EventTraits = ["MVP", "Opponent (Mutual)"],
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "PS=10",
                Test = Ps10(),
                Description = "...do nothing and see what happens",
                Traits = [T(TraitKind.Xp, "XP"), T(TraitKind.Cr, "CR (Neg)", negated: true)]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...convince them to stop seeing each other",
                Traits = [T(TraitKind.Cr, "CR")]
            },
            new DowntimeChoice
            {
                TestLabel = "PH",
                Test = Stat("PH", 0, "PH"),
                Description = "...knock some sense into them",
                Traits = [T(TraitKind.Xp, "XP"), T(TraitKind.Attack, "Attack")]
            }
        ]
    };

    private static readonly DowntimeEvent Event7To8 = new()
    {
        RollRange = "7-8",
        RepresentativeRoll = 7,
        Description = "You get wind that a Rival Mercs team is planning an ambush on your team after some unfinished business from the most recent contract...",
        EventTraits = ["Opponent"],
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "CC-10",
                Test = Stat("CC", -10, "CC-10"),
                Description = "...hide in wait and counter ambush them",
                Traits = [T(TraitKind.Attack, "Attack"), T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...avoid the confrontation",
                Traits = [T(TraitKind.Skill, "Skill (Stealth)", detail: "Stealth"), T(TraitKind.Attack, "Attack")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...report them to the cops",
                Traits = [T(TraitKind.Lawful, "Lawful")]
            }
        ]
    };

    private static readonly DowntimeEvent Event9To10 = new()
    {
        RollRange = "9-10",
        RepresentativeRoll = 9,
        Description = "Select a Merc that went unconscious this contract. A rival Mercs team has taken them hostage after the most recent contract...",
        EventTraits = ["Opponent"],
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...you negotiate a ransom",
                Traits = [T(TraitKind.P2P, "P2P"), T(TraitKind.Lawful, "Lawful"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "BS",
                Test = Stat("BS", 0, "BS"),
                Description = "...you ambush them at the drop site",
                Traits = [T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Attack, "Attack"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "PH",
                Test = Stat("PH", 0, "PH"),
                Description = "...let the Merc escape on their own",
                Traits = [T(TraitKind.Xp, "XP"), T(TraitKind.Cr, "CR (Neg)", negated: true)]
            }
        ]
    };

    private static readonly DowntimeEvent Event11To12 = new()
    {
        RollRange = "11-12",
        RepresentativeRoll = 11,
        Description = "One of your Mercs has decided he's better suited to be the captain...",
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "PS=10",
                Test = Ps10(),
                Description = "...allow the change of leadership",
                Traits = [T(TraitKind.Cr, "CR (Neg)", negated: true), T(TraitKind.Lt, "LT")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...negotiate a new salary",
                Traits = [T(TraitKind.Captain, "Captain"), T(TraitKind.Xp, "XP"), T(TraitKind.P2P, "P2P")]
            },
            new DowntimeChoice
            {
                TestLabel = "CC-10",
                Test = Stat("CC", -10, "CC-10"),
                Description = "...agni kai",
                Traits = [T(TraitKind.Captain, "Captain"), T(TraitKind.Attack, "Attack"), T(TraitKind.Chaotic, "Chaotic")]
            }
        ]
    };

    private static readonly DowntimeEvent Event13To14 = new()
    {
        RollRange = "13-14",
        RepresentativeRoll = 13,
        Description = "The Merc with the highest renown must be selected for this event. They have run into legal trouble and must present themselves in court...",
        RequiredParticipant = ParticipantKind.Renowned,
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "PS=10",
                Test = Ps10(),
                Description = "...you settle the issue out of court",
                Traits = [T(TraitKind.P2P, "P2P"), T(TraitKind.Xp, "XP")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...they defend themselves in court",
                Traits = [T(TraitKind.Cr, "CR"), T(TraitKind.Lawful, "Lawful")]
            },
            new DowntimeChoice
            {
                TestLabel = "CC-10",
                Test = Stat("CC", -10, "CC-10"),
                Description = "...they attempt to intimidate the opposing legal team",
                Traits = [T(TraitKind.Cr, "CR"), T(TraitKind.Chaotic, "Chaotic")]
            }
        ]
    };

    private static readonly DowntimeEvent Event15To16 = new()
    {
        RollRange = "15-16",
        RepresentativeRoll = 15,
        Description = "You left behind your work laptop at the last contract and the opposing Merc team took it to see what they could gain...",
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...you ask nicely for it back with some incentive",
                Traits = [T(TraitKind.P2P, "P2P"), T(TraitKind.Swc, "SWC"), T(TraitKind.Lawful, "Lawful")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...you hack the computer to shut it down",
                Traits = [T(TraitKind.Attack, "Attack"), T(TraitKind.Xp, "XP"), T(TraitKind.Requirement, "Requirement (Hacker)", detail: "Hacker")]
            },
            new DowntimeChoice
            {
                TestLabel = "WIP",
                Test = Stat("WIP", 0, "WIP"),
                Description = "...you counter hack whoever is on the other end",
                Traits = [T(TraitKind.Attack, "Attack"), T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Xp, "XP"), T(TraitKind.Requirement, "Requirement (Trinity Program)", detail: "Trinity Program")]
            }
        ]
    };

    private static readonly DowntimeEvent Event17To18 = new()
    {
        RollRange = "17-18",
        RepresentativeRoll = 17,
        Description = "Your team gets caught into a brawl after some heated discussion turns to fist to cuffs...",
        Choices =
        [
            new DowntimeChoice
            {
                TestLabel = "PH",
                Test = Stat("PH", 0, "PH"),
                Description = "...you intimidate them to stop fighting",
                Traits = [T(TraitKind.Attack, "Attack"), T(TraitKind.Lawful, "Lawful")]
            },
            new DowntimeChoice
            {
                TestLabel = "CC-10",
                Test = Stat("CC", -10, "CC-10"),
                Description = "...you revel in the chaos",
                Traits = [T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Skill, "Skill (Natural Born Warrior)", detail: "Natural Born Warrior"), T(TraitKind.Attack, "Attack")]
            },
            new DowntimeChoice
            {
                TestLabel = "BS",
                Test = Stat("BS", 0, "BS"),
                Description = "...you bring a gun to a fist fight",
                Traits = [T(TraitKind.Chaotic, "Chaotic"), T(TraitKind.Cr, "CR")]
            }
        ]
    };

    private static readonly DowntimeEvent Event19To20 = new()
    {
        RollRange = "19-20",
        Description = "No incident occurs. You may reroll until you get an incident if you wish or choose to have no event.",
        IsNoIncident = true
    };
}
