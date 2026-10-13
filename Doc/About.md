# Trast File Player

The Trast File Player executes trajectories on Triamec Servo Drives. It provides a straightforward way to execute demanding, synchronized motion sequences with a sampling rate of up to 50kHz.

To develop a custom application using the TraSt framework, refer to the TAM Software documentation.

## Operation

First, open a TraSt data file with the **Open** button. The Trast File Player verifies the file format and the referenced axes and devices.

After the file has loaded successfully:

1. **Enable** the axes.
2. **Home** the axes, if required.
3. Use **Init Move** to move all involved axes to the initial trajectory position.
4. Start the trajectory with **Play**.

The **Feed Override** can be adjusted at any time during trajectory execution. It controls the execution speed as a percentage. Use **Pause** to pause the trajectory without ending it, and use **Play** to continue. Use **Stop** to stop execution.

When **Loop** is enabled, the trajectory is repeated after it reaches its end. If the final and initial positions differ, the axes first move back to the initial position before the next cycle starts.

If an involved axis or device enters a fault state, trajectory execution stops. Use **Reset Error** to acknowledge faults.

## TraSt CSV File Format

A TraSt file consists of a **header** followed by **trajectory data**. The header describes the file, the involved axes, and the assignment of data columns. It always ends with:

```text
--EndOfHeader--
```

### Header Rules

- Comments start with `#` or `//`. Everything after the comment marker is ignored.
- Define one parameter per line. Empty lines are allowed.
- Separate the parameter name and value with at least one space or tab character.
- Enclose string values in double quotation marks, for example, `"Axis X"`.
- General parameters start with `gp.` and must appear before axis parameters.
- Axis parameters start with `ax[n].`, where `n` is the index used to group parameters to a dedicated axis.
- Column indices are zero-based.

### General Parameters

| Parameter | Description |
| --- | --- |
| `gp.FileFormatVersion` | Version number of the file format. |
| `gp.NumberOfDataRows` | Number of trajectory data rows following the header. |
| `gp.SamplingFrequencyHz` | Sampling frequency of the trajectory data in Hz. |

### Axis Parameters

For each involved axis, specify at least its identifier and its position column.

| Parameter | Description |
| --- | --- |
| `ax[n].AxisIdentifier` | Axis name, for example, `"Axis X"`. |
| `ax[n].PositionColumnIndex` | Column containing the axis position data. |
| `ax[n].CurrentColumnIndex` | Optional column containing feed-forward current data. |
| `ax[n].VelocityColumnIndex` | Optional column containing velocity data for a velocity extension. |
| `ax[n].CustomExtensionColumnIndex[...]` | Optional column containing extension data. |

Optional data is not used when its corresponding column parameter is omitted.

### Trajectory Data

The data rows after `--EndOfHeader--` use comma-separated values:

- Each row represents one trajectory sample.
- Values in a row are separated by commas.
- The header's column indices assign values to position, current, velocity, and extension data.
- The number of data rows must match `gp.NumberOfDataRows`.

### Example

```ini
# General Parameters
gp.FileFormatVersion        1
gp.NumberOfDataRows         1500
gp.NumberOfInvolvedAxes     2
gp.SamplingFrequencyHz      50000

# Axis Parameters
ax[0].AxisIdentifier        "Axis X"
ax[0].PositionColumnIndex   0
ax[0].CurrentColumnIndex    1
ax[1].AxisIdentifier        "Axis Y"
ax[1].PositionColumnIndex   2

--EndOfHeader--

0.000, 0.000, 0.000
0.000, 0.000, 0.000
0.000, 0.000, 0.000
...
```

## Support

For support, contact <support@triamec.com>.
