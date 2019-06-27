import React from 'react';

const DataCell = React.memo((props) => {
  const { column, value } = props;

  var cellData = null;

  if (!column.render) {
    cellData = (column.data ? value : '')
  } else if (column.render instanceof Function || column.render['$$typeof'] === Symbol.for('react.memo')) {
    const CustomRenderClass = column.render;
    cellData = <CustomRenderClass {...props} />
  } else if (column.render.component) {
    const { component: CustomRenderClass, ...renderProps } = column.render;
    cellData = <CustomRenderClass {...props} {...renderProps} />
  }

  return <td className={column.className}>{cellData}</td>
})

export default DataCell