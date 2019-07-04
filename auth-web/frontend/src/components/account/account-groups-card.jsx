import React, { Component } from 'react';
import { Button } from 'reactstrap';

class GroupsByAssignmentCard extends Component {
  constructor(props) {
    super(props)
    this.state = { expanded: { } }
  }

  toggleExpanded = id => {
    const expanded = { ...this.state.expanded };
    if (expanded[id]) {
      delete expanded[id]
    } else {
      expanded[id] = true;
    }
    this.setState({ expanded })
  }

  render() {
    const {groups} = this.props;
    const { expanded } = this.state;
    return (
      <div>
        {groups.data.map(group => group.relationships.parent
          ? expanded[group.relationships.parent.data.id]
            ? <div key={`${group.id}:${group.relationships.parent.data.id}`} className='sub-group'>{group.id}</div>
            : null
          : <div key={group.id}><Button className='expando' color='link' onClick={() => this.toggleExpanded(group.id)}><i className={`far fa-${expanded[group.id] ? 'minus' : 'plus'}-square`}></i></Button> <strong>{group.attributes.displayName}</strong></div>
        )}
      </div>
    );
  }
}

class GroupsByEffectiveCard extends Component {
  render() {
    const { groups } = this.props;
    const part = groups.data.map(g => g.relationships.parent ? g.relationships.parent.data : g )
console.log(part);

    const effective = Object.values(
                        groups.data
                          //.map(g => g.relationships.parent ? g.relationships.parent.data : g )
                          .reduce((accum, cur) => {
                            accum[cur.id] = {...accum[cur.id], ...cur};
                            return accum;
                          }, {})
                      );
    effective.sort((a,b) => a.id < b.id ? -1 : a.id > b.id ? 1 : 0);
    
    console.log(effective)
    return <div>
      {effective.map(g => <div key={g.id}><strong>{g.id}</strong></div>)}
    </div>
  }
}

export default class AccountGroupsCard extends Component {
  constructor(props) {
    super(props);
    this.state = { mode: 'assigned' }
  }

  setMode = (event) => {
    this.setState({mode: event.target.value })
  }

  render() {
    const {groups} = this.props;
    return (
      <div className='card'>
        <h6>Groups</h6>
        <div className='form-inline'>Display: <select className='form-control-sm' value={this.state.mode} onChange={this.setMode}>
          <option value='assigned'>Assigned</option>
          <option value='effective'>Effective</option>
        </select></div>
        {groups && groups.data
          ? groups.data.length
            ? this.state.mode === 'assigned' ? <GroupsByAssignmentCard groups={groups} /> : <GroupsByEffectiveCard groups={groups} />
            : <div><i>Not a member of any groups</i></div>
          : <div><i className="fas fa-spin fa-spinner"></i></div>}
      </div>
    );
  }
}