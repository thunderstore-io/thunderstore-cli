using System;
using System.Threading.Tasks;
using Xunit;

namespace ThunderstoreCLI.Tests;

public class ThunderstoreCLI_ProgresSpinner
{
    private async Task CreateTask(bool isSuccess, int delay = 1)
    {
        await Task.Delay(delay);

        if (!isSuccess)
        {
            throw new Exception();
        }
    }

    [Fact]
    public void WhenInitiatedWithoutTasks_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new ProgressSpinner("", new Task[] { }));
    }

    [Fact]
    public async Task WhenTaskFails_ThrowsSpinnerException()
    {
        var spinner = new ProgressSpinner("", new[] {
            CreateTask(false)
        });

        await Assert.ThrowsAsync<SpinnerException>(async () => await spinner.Spin());
    }

    [Fact]
    public async Task WhenReceivesSingleTask_ItJustWorks()
    {
        var spinner = new ProgressSpinner("", new[] {
            CreateTask(true)
        });

        await spinner.Spin();
    }

    [Fact]
    public async Task WhenReceivesMultipleTasks_ItJustWorks()
    {
        var spinner = new ProgressSpinner("", new[] {
            CreateTask(true),
            CreateTask(true),
            CreateTask(true)
        });

        await spinner.Spin();
    }
}
