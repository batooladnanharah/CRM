// Mini Task Manager - TaskSummary
// A simple presentational component that receives already-computed
// numbers through props.

type TaskSummaryProps = {
  completedCount: number;
  remainingCount: number;
};

function TaskSummary({ completedCount, remainingCount }: TaskSummaryProps) {
  return (
    <div className="task-summary">
      <span>Completed: {completedCount}</span>
      <span>Remaining: {remainingCount}</span>
    </div>
  );
}

export default TaskSummary;
